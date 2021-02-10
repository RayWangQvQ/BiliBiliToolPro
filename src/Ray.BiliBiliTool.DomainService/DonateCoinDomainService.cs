using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 投币
    /// </summary>
    public class DonateCoinDomainService : IDonateCoinDomainService
    {
        private readonly ILogger<DonateCoinDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliCookie _biliBiliCookie;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly IAccountApi _accountApi;
        private readonly ICoinDomainService _coinDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IRelationApi _relationApi;
        private readonly IVideoApi _videoApi;
        private readonly Dictionary<string, int> _expDic;
        private readonly Dictionary<string, string> _donateContinueStatusDic;

        /// <summary>
        /// up的视频稿件总数缓存
        /// </summary>
        private readonly Dictionary<long, int> _upVideoCountDicCatch = new Dictionary<long, int>();

        /// <summary>
        /// 已对视频投币数量缓存
        /// </summary>
        private readonly Dictionary<string, int> _alreadyDonatedCoinCountCatch = new Dictionary<string, int>();

        public DonateCoinDomainService(
            ILogger<DonateCoinDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliCookie cookie,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IAccountApi accountApi,
            ICoinDomainService coinDomainService,
            IVideoDomainService videoDomainService,
            IRelationApi relationApi,
            IOptionsMonitor<Dictionary<string, int>> expDicOptions,
            IOptionsMonitor<Dictionary<string, string>> donateContinueStatusDicOptions,
            IVideoApi videoApi
            )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookie = cookie;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _accountApi = accountApi;
            _coinDomainService = coinDomainService;
            _videoDomainService = videoDomainService;
            _relationApi = relationApi;
            _videoApi = videoApi;
            _expDic = expDicOptions.Get(Constants.OptionsNames.ExpDictionaryName);
            _donateContinueStatusDic = donateContinueStatusDicOptions.Get(Constants.OptionsNames.DonateCoinCanContinueStatusDictionaryName);
        }

        /// <summary>
        /// 完成投币任务
        /// </summary>
        public void AddCoinsForVideos()
        {
            int needCoins = GetNeedDonateCoinNum();
            if (needCoins <= 0) return;

            //投币前硬币余额
            decimal coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("投币前余额为 : {coinBalance}", coinBalance);

            if (coinBalance <= 0)
            {
                _logger.LogInformation("因硬币余额不足，今日暂不执行投币任务");
                return;
            }

            //余额小于目标投币数，按余额投
            if (coinBalance < needCoins)
            {
                int.TryParse(decimal.Truncate(coinBalance).ToString(), out needCoins);
                _logger.LogInformation("因硬币余额不足，目标投币数调整为: {needCoins}", needCoins);
            }

            int success = 0;
            int tryCount = 10;
            for (int i = 1; i <= tryCount && success < needCoins; i++)
            {
                _logger.LogDebug("开始尝试第{num}次", i);

                Tuple<string, string> video = TryGetCanDonatedVideo();
                if (video == null) continue;

                _logger.LogDebug("正在为视频“{title}”投币", video.Item2);

                bool re = DoAddCoinForVideo(video.Item1, 1, _dailyTaskOptions.SelectLike, video.Item2);
                if (re) success++;
            }

            if (success == needCoins)
                _logger.LogInformation("投币任务完成，余额为: {money}", _accountApi.GetCoinBalance().GetAwaiter().GetResult().Data.Money ?? 0);
            else
                _logger.LogInformation("投币尝试超过10次，已终止");
        }

        /// <summary>
        /// 尝试获取一个可以投币的视频
        /// </summary>
        /// <returns></returns>
        public Tuple<string, string> TryGetCanDonatedVideo()
        {
            Tuple<string, string> result = null;

            //从配置的up中随机尝试获取1次
            result = TryGetCanDonateVideoByConfigUps(1);
            if (result != null) return result;

            //然后从特别关注列表尝试获取1次
            result = TryGetCanDonateVideoBySpecialUps(1);
            if (result != null) return result;

            //然后从普通关注列表获取1次
            result = TryGetCanDonateVideoByFollowingUps(1);
            if (result != null) return result;

            //最后从排行榜尝试5次
            result = TryGetCanDonateVideoByRegion(5);

            return result;
        }

        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <param name="multiply">投币数量</param>
        /// <param name="select_like">是否同时点赞 1是0否</param>
        /// <returns>是否投币成功</returns>
        public bool DoAddCoinForVideo(string aid, int multiply, bool select_like, string title = "")
        {
            BiliApiResponse result;
            try
            {
                var request = new AddCoinRequest(long.Parse(aid), _biliBiliCookie.BiliJct);
                request.Multiply = multiply;
                request.Select_like = select_like ? 1 : 0;
                result = _videoApi.AddCoinForVideo(request)
                    .GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return false;
            }

            if (result.Code == 0)
            {
                _expDic.TryGetValue("每日投币", out int exp);
                _logger.LogInformation("为“{title}”投币成功，经验+{exp} √", title, exp);
                return true;
            }

            if (_donateContinueStatusDic.Any(x => x.Key == result.Code.ToString()))
            {
                _logger.LogError("尝试为“{title}”投币失败，原因：{msg}", title, result.Message);
                return false;
            }

            else
            {
                string errorMsg = $"投币发生未预计异常。接口返回：{result.Message}";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        #region private

        /// <summary>
        /// 获取今日的目标投币数
        /// </summary>
        /// <returns></returns>
        private int GetNeedDonateCoinNum()
        {
            //获取自定义配置投币数
            int configCoins = _dailyTaskOptions.NumberOfCoins;
            if (configCoins <= 0)
            {
                _logger.LogInformation("已配置为跳过投币任务");
                return configCoins;
            }

            //已投的硬币
            int alreadyCoins = _coinDomainService.GetDonatedCoins();
            //目标
            int targetCoins = configCoins > Constants.MaxNumberOfDonateCoins
                ? Constants.MaxNumberOfDonateCoins
                : configCoins;

            if (targetCoins > alreadyCoins)
            {
                int needCoins = targetCoins - alreadyCoins;
                _logger.LogInformation("今日已投{already}枚硬币，目标是投{target}枚，还需再投{need}枚", alreadyCoins, targetCoins, needCoins);
                return needCoins;
            }

            _logger.LogInformation("今日已投{already}枚硬币，已完成投币任务，不需要再投啦~", alreadyCoins);
            return 0;
        }

        /// <summary>
        /// 尝试从配置的up主里随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryGetCanDonateVideoByConfigUps(int tryCount)
        {
            //是否配置了up主
            if (_dailyTaskOptions.SupportUpIdList.Count == 0) return null;

            return TryCanDonateVideoByUps(_dailyTaskOptions.SupportUpIdList, tryCount); ;
        }

        /// <summary>
        /// 尝试从特别关注的Up主中随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryGetCanDonateVideoBySpecialUps(int tryCount)
        {
            //获取特别关注列表
            var request = new GetSpecialFollowingsRequest(long.Parse(_biliBiliCookie.UserId));
            BiliApiResponse<List<UpInfo>> specials = _relationApi.GetSpecialFollowings(request)
                .GetAwaiter().GetResult();
            if (specials.Data == null || specials.Data.Count == 0) return null;

            return TryCanDonateVideoByUps(specials.Data.Select(x => x.Mid).ToList(), tryCount);
        }

        /// <summary>
        /// 尝试从普通关注的Up主中随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryGetCanDonateVideoByFollowingUps(int tryCount)
        {
            //获取特别关注列表
            var request = new GetFollowingsRequest(long.Parse(_biliBiliCookie.UserId));
            BiliApiResponse<GetFollowingsResponse> result = _relationApi.GetFollowings(request)
                .GetAwaiter().GetResult();
            if (result.Data.Total == 0) return null;

            return TryCanDonateVideoByUps(result.Data.List.Select(x => x.Mid).ToList(), tryCount);
        }

        /// <summary>
        /// 尝试从排行榜中获取一个没有看过的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryGetCanDonateVideoByRegion(int tryCount)
        {
            for (int i = 0; i < tryCount; i++)
            {
                var video = _videoDomainService.GetRandomVideoOfRanking();
                if (!IsCanDonate(video.Aid.ToString())) continue;
                return Tuple.Create<string, string>(video.Aid.ToString(), video.Title);
            }

            return null;
        }

        /// <summary>
        /// 尝试从指定的up主集合中随机获取一个可以尝试投币的视频
        /// </summary>
        /// <param name="upIds"></param>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryCanDonateVideoByUps(List<long> upIds, int tryCount)
        {
            if (upIds == null || upIds.Count == 0) return null;

            //尝试tryCount次
            for (int i = 1; i <= tryCount; i++)
            {
                //获取随机Up主Id
                long randomUpId = upIds[new Random().Next(0, upIds.Count)];

                if (randomUpId == 0 || randomUpId == long.MinValue) continue;

                if (randomUpId.ToString() == _biliBiliCookie.UserId)
                {
                    _logger.LogDebug("不能为自己投币");
                    continue;
                }

                //该up的视频总数
                if (!_upVideoCountDicCatch.TryGetValue(randomUpId, out int videoCount))
                {
                    videoCount = _videoDomainService.GetVideoCountOfUp(randomUpId);
                    _upVideoCountDicCatch.Add(randomUpId, videoCount);
                }
                if (videoCount == 0) continue;

                UpVideoInfo videoInfo = _videoDomainService.GetRandomVideoOfUp(randomUpId, videoCount);
                _logger.LogDebug("获取到视频{aid}({title})", videoInfo.Aid, videoInfo.Title);

                //检查是否可以投
                if (!IsCanDonate(videoInfo.Aid.ToString())) continue;

                return Tuple.Create(videoInfo.Aid.ToString(), videoInfo.Title);
            }

            return null;
        }

        /// <summary>
        /// 已为视频投币个数是否小于最大限制
        /// </summary>
        /// <param name="aid">av号</param>
        /// <returns></returns>
        private bool IsDonatedLessThenLimitCoinsForVideo(string aid)
        {
            //获取已投币数量
            if (!_alreadyDonatedCoinCountCatch.TryGetValue(aid, out int multiply))
            {
                multiply = _videoApi.GetDonatedCoinsForVideo(new GetAlreadyDonatedCoinsRequest(long.Parse(aid)))
                    .GetAwaiter().GetResult()
                    .Data.Multiply;
                _alreadyDonatedCoinCountCatch.TryAdd(aid, multiply);
            }

            _logger.LogDebug("已为Av{aid}投过{num}枚硬币", aid, multiply);

            if (multiply >= 2) return false;

            //获取该视频可投币数量
            int limitCoinNum = _videoDomainService.GetVideoDetail(aid).Copyright == 1
                ? 2 //原创，最多可投2枚
                : 1;//转载，最多可投1枚
            _logger.LogDebug("该视频的最大投币数为{num}", limitCoinNum);

            return multiply < limitCoinNum;
        }

        /// <summary>
        /// 检查获取到的视频是否可以投币
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        private bool IsCanDonate(string aid)
        {
            //本次运行已经尝试投过的,不进行重复投（不管成功还是失败，凡取过尝试过的，不重复尝试）
            if (_alreadyDonatedCoinCountCatch.Any(x => x.Key == aid))
            {
                _logger.LogDebug("重复视频，丢弃处理");
                return false;
            }

            //已经投满2个币的，不能再投
            if (!IsDonatedLessThenLimitCoinsForVideo(aid))
            {
                _logger.LogDebug("超出单个视频投币数量限制，丢弃处理", aid);
                return false;
            }

            return true;
        }

        #endregion
    }
}
