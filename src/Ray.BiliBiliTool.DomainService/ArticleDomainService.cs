using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
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

    public ArticleDomainService(IArticleApi articleApi, BiliCookie biliCookie, ILogger<ArticleDomainService> logger, IOptionsMonitor<DailyTaskOptions> dailyTaskOptions, ICoinDomainService coinDomainService)
    {
        _articleApi = articleApi;
        _biliCookie = biliCookie;
        _logger = logger;
        _coinDomainService = coinDomainService;
        _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    }

    /// <summary>
    /// 投币专栏任务
    /// </summary>
    /// <returns></returns>
    public async Task AddCoinForArticles()
    {
        var donateCoinsCounts = await CalculateDonateCoinsCounts();

        if (donateCoinsCounts == 0)
        {
            return;
        }


            
    }



    /// <summary>
    /// 给某一篇专栏投币
    /// </summary>
    /// <param name="cvid">文章cvid</param>
    /// <param name="mid">文章作者mid</param>
    /// <returns>投币是否成功</returns>
    public async Task<bool> AddCoinForArticle(long cvid, long mid)
    {
        BiliApiResponse result;
        try
        {
            var refer = $"https://www.bilibili.com/read/cv{cvid}/?from=search&spm_id_from=333.337.0.0";
            result = await _articleApi.AddCoinForArticle(new AddCoinForArticleRequest(cvid, mid, _biliCookie.BiliJct),
                refer);
        }
        catch (Exception )
        {
            return false;
        }
        
        if (result.Code == 0)
        {
            _logger.LogInformation("投币成功，经验+10 √" );
            return true;
        }
        else
        {
            _logger.LogError("投币错误 {message}", result.Message);
            return false;
        }
    }


    /// <summary>
    /// 计算所需要投的硬币数量
    /// </summary>
    /// <returns>硬币数量</returns>
    public async Task<int> CalculateDonateCoinsCounts()
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

        return 0;
    }

    public async Task<int> GetNeedDonateCoinCounts()
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
}
