using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

public class ArticleDomainService : IArticleDomainService
{
    private readonly IArticleApi _articleApi;
    private readonly BiliCookie _biliCookie;
    private readonly ILogger<ArticleDomainService> _logger;
    private readonly DailyTaskOptions _dailyTaskOptions;
    private readonly ICoinDomainService _coinDomainService;
    private readonly IAccountApi _accountApi;
    private readonly IWbiService _wbiService;


    /// <summary>
    /// up的专栏总数缓存
    /// </summary>
    private readonly Dictionary<long, int> _upArticleCountDicCatch = new();

    /// <summary>
    /// 已对投币数量缓存
    /// </summary>
    private readonly Dictionary<string, int> _alreadyDonatedCoinCountCatch = new();

    public ArticleDomainService(
        IArticleApi articleApi,
        BiliCookie biliCookie,
        ILogger<ArticleDomainService> logger,
        IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
        ICoinDomainService coinDomainService,
        IAccountApi accountApi, IWbiService wbiService)
    {
        _articleApi = articleApi;
        _biliCookie = biliCookie;
        _logger = logger;
        _coinDomainService = coinDomainService;
        _accountApi = accountApi;
        _wbiService = wbiService;
        _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    }


    public async Task LikeArticle(long cvid)
    {
        await _articleApi.LikeAsync(cvid, _biliCookie.BiliJct);
    }

    /// <summary>
    /// 投币专栏任务
    /// </summary>
    /// <returns></returns>
    public async Task<bool> AddCoinForArticles()
    {
        var donateCoinsCounts = await CalculateDonateCoinsCounts();

        if (donateCoinsCounts == 0)
        {
            // 没有可投的币相当于投币任务全部完成
            return true;
        }


        int success = 0;
        int tryCount = 10;

        for (int i = 0; i <= tryCount && success < donateCoinsCounts; i++)
        {
            _logger.LogDebug("开始尝试第{num}次", i);

            var upId = GetUpFromConfigUps();
            if (upId == 0)
            {
                _logger.LogDebug("未能成功选择支持的Up主");
                continue;
            }
            // 当upId不符合时，会直接报错，需要将两者的判断分隔开
            var cvid = await GetRandomArticleFromUp(upId);
            if (cvid == 0)
            {
                _logger.LogDebug("第{num}次尝试，未能成功选择合适的专栏",i);
                continue;
            }

            if (await AddCoinForArticle(cvid, upId))
            {
                // 点赞
                if (_dailyTaskOptions.SelectLike)
                {
                    await LikeArticle(cvid);
                    _logger.LogInformation("专栏点赞成功");
                }

                success++;
            }
        }

        if (success == donateCoinsCounts)
            _logger.LogInformation("专栏投币任务完成");
        else
        {
            _logger.LogInformation("投币尝试超过10次，已终止");
            return false;
        }


        _logger.LogInformation("【硬币余额】{coin}", (await _accountApi.GetCoinBalanceAsync()).Data.Money ?? 0);

        return true;
    }


    /// <summary>
    /// 给某一篇专栏投币
    /// </summary>
    /// <param name="cvid">文章cvid</param>
    /// <param name="mid">文章作者mid</param>
    /// <returns>投币是否成功（false 投币失败，true 投币成功）</returns>
    public async Task<bool> AddCoinForArticle(long cvid, long mid)
    {
        BiliApiResponse result;
        try
        {
            var refer = $"https://www.bilibili.com/read/cv{cvid}/?from=search&spm_id_from=333.337.0.0";
            result = await _articleApi.AddCoinForArticleAsync(new AddCoinForArticleRequest(cvid, mid, _biliCookie.BiliJct),
                refer);
        }
        catch (Exception)
        {
            return false;
        }

        if (result.Code == 0)
        {
            _logger.LogInformation("投币成功，经验+10 √");
            return true;
        }
        else
        {
            _logger.LogError("投币错误 {message}", result.Message);
            return false;
        }
    }


    #region private

    /// <summary>
    /// 从某个up主中随机挑选一个专栏
    /// </summary>
    /// <param name="mid"></param>
    /// <returns>专栏的cvid</returns>
    private async Task<long> GetRandomArticleFromUp(long mid)
    {
        if (!_upArticleCountDicCatch.TryGetValue(mid, out int articleCount))
        {
            articleCount = await GetArticleCountOfUp(mid);
            _upArticleCountDicCatch.Add(mid, articleCount);
        }

        // 专栏数为0时
        if (articleCount == 0)
        {
            return 0;
        }

        var req = new SearchArticlesByUpIdDto()
        {
            mid = mid,
            ps = 1,
            pn = new Random().Next(1, articleCount + 1)
        };
        await _wbiService.SetWridAsync(req);

        BiliApiResponse<SearchUpArticlesResponse> re = await _articleApi.SearchUpArticlesByUpIdAsync(req);

        if (re.Code != 0)
        {
            throw new Exception(re.Message);
        }

        ArticleInfo articleInfo = re.Data.Articles.FirstOrDefault();

        _logger.LogInformation("获取到的专栏{cvid}({title})", articleInfo.Id, articleInfo.Title);

        // 检查是否可投
        if (!await IsCanDonate(articleInfo.Id))
        {
            return 0;
        }

        return articleInfo.Id;
    }


    // TODO 转变为异步代码
    /// <summary>
    /// 从支持UP主列表中随机挑选一位
    /// </summary>
    /// <returns>被挑选up主的mid</returns>
    private long GetUpFromConfigUps()
    {
        if (_dailyTaskOptions.SupportUpIdList == null || _dailyTaskOptions.SupportUpIdList.Count == 0)
        {
            return 0;
        }

        try
        {
            long randomUpId =
                _dailyTaskOptions.SupportUpIdList[new Random().Next(0, _dailyTaskOptions.SupportUpIdList.Count)];

            if (randomUpId is 0 or long.MinValue) return 0;

            if (randomUpId.ToString() == _biliCookie.UserId)
            {
                _logger.LogDebug("不能为自己投币");
                return 0;
            }

            _logger.LogDebug("挑选出的up主为{UpId}", randomUpId);
            return randomUpId;
        }
        catch (Exception e)
        {
            _logger.LogWarning("异常：{msg}", e);
        }

        return 0;
    }

    /// <summary>
    /// 获取Up主专栏总数
    /// </summary>
    /// <param name="mid">up主mid</param>
    /// <returns>专栏总数</returns>
    /// <exception cref="Exception"></exception>
    private async Task<int> GetArticleCountOfUp(long mid)
    {
        var req = new SearchArticlesByUpIdDto()
        {
            mid = mid
        };

        await _wbiService.SetWridAsync(req);

        BiliApiResponse<SearchUpArticlesResponse> re = await _articleApi.SearchUpArticlesByUpIdAsync(req);

        if (re.Code != 0)
        {
            throw new Exception(re.Message);
        }

        return re.Data.Count;
    }

    /// <summary>
    /// 计算所需要投的硬币数量
    /// </summary>
    /// <returns>硬币数量</returns>
    private async Task<int> CalculateDonateCoinsCounts()
    {
        int needCoins = await GetNeedDonateCoinCounts();

        int protectedCoins = _dailyTaskOptions.NumberOfProtectedCoins;
        if (needCoins <= 0) return 0;

        //投币前硬币余额
        decimal coinBalance = await _coinDomainService.GetCoinBalance();
        _logger.LogInformation("【投币前余额】 : {coinBalance}", coinBalance);
        _ = int.TryParse(decimal.Truncate(coinBalance - protectedCoins).ToString(), out int unprotectedCoins);

        if (coinBalance <= 0)
        {
            _logger.LogInformation("因硬币余额不足，今日暂不执行投币任务");
            return 0;
        }

        if (coinBalance <= protectedCoins)
        {
            _logger.LogInformation("因硬币余额达到或低于保留值，今日暂不执行投币任务");
            return 0;
        }

        //余额小于目标投币数，按余额投
        if (coinBalance < needCoins)
        {
            _ = int.TryParse(decimal.Truncate(coinBalance).ToString(), out needCoins);
            _logger.LogInformation("因硬币余额不足，目标投币数调整为: {needCoins}", needCoins);
            return needCoins;
        }

        //投币后余额小于等于保护值，按保护值允许投
        if (coinBalance - needCoins <= protectedCoins)
        {
            //排除需投等于保护后可投数量相等时的情况
            if (unprotectedCoins != needCoins)
            {
                needCoins = unprotectedCoins;
                _logger.LogInformation("因硬币余额投币后将达到或低于保留值，目标投币数调整为: {needCoins}", needCoins);
                return needCoins;
            }
        }

        return needCoins;
    }

    private async Task<int> GetNeedDonateCoinCounts()
    {
        int configCoins = _dailyTaskOptions.NumberOfCoins;

        if (configCoins <= 0)
        {
            _logger.LogInformation("已配置为跳过投币任务");
            return configCoins;
        }

        //已投的硬币
        int alreadyCoins = await _coinDomainService.GetDonatedCoins();

        int targetCoins = configCoins;

        _logger.LogInformation("【今日已投】{already}枚", alreadyCoins);
        _logger.LogInformation("【目标欲投】{already}枚", targetCoins);

        if (targetCoins > alreadyCoins)
        {
            int needCoins = targetCoins - alreadyCoins;
            _logger.LogInformation("【还需再投】{need}枚", needCoins);
            return needCoins;
        }

        _logger.LogInformation("已完成投币任务，不需要再投啦~");
        return 0;
    }


    private async Task<bool> IsCanDonate(long cvid)
    {
        try
        {
            if (_alreadyDonatedCoinCountCatch.Any(x => x.Key == cvid.ToString()))
            {
                _logger.LogDebug("重复专栏，丢弃处理");
                return false;
            }

            if (!_alreadyDonatedCoinCountCatch.TryGetValue(cvid.ToString(), out int multiply))
            {
                multiply = (await _articleApi.SearchArticleInfoAsync(cvid)).Data.Coin;
                _alreadyDonatedCoinCountCatch.TryAdd(cvid.ToString(), multiply);
            }

            // 在网页端我测试时只能投一枚硬币，暂时设置最多投一枚
            if (multiply >= 1)
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning("异常：{mag}", e);
            return false;
        }
    }

    #endregion
}
