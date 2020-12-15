using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 投币
    /// </summary>
    public class DonateCoinDomainService : IDonateCoinDomainService
    {
        private readonly ILogger<DonateCoinDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookieOptions _biliBiliCookieOptions;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly IAccountApi _accountApi;
        private readonly ICoinDomainService _coinDomainService;
        private readonly IRelationApi _relationApi;

        public DonateCoinDomainService(ILogger<DonateCoinDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliBiliCookieOptions biliBiliCookieOptions,
            DailyTaskOptions dailyTaskOptions,
            IAccountApi accountApi,
            ICoinDomainService coinDomainService,
            IRelationApi relationApi)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookieOptions = biliBiliCookieOptions;
            _dailyTaskOptions = dailyTaskOptions;
            _accountApi = accountApi;
            _coinDomainService = coinDomainService;
            _relationApi = relationApi;
        }

        /// <summary>
        /// 完成投币任务
        /// </summary>
        public void AddCoinsForVideo()
        {
            int needCoins = GetNeedDonateCoinNum(out int alreadyCoins, out int targetCoins);
            _logger.LogInformation("今日已投{already}枚硬币，目标是投{target}枚硬币", alreadyCoins, targetCoins);

            if (needCoins <= 0)
            {
                _logger.LogInformation("已经完成投币任务，今天不需要再投啦");
                return;
            }

            _logger.LogInformation("还需再投{need}枚硬币", needCoins);

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

            int successCoins = 0;
            int tryCount = 0;//投币最多操作数 解决异常导致死循环的问题
            while (successCoins < needCoins)
            {
                tryCount++;

                Tuple<string, string> video = TryGetCanDonatedVideo();
                if (video == null)
                {
                    if (tryCount <= 10) continue;
                    _logger.LogInformation("尝试投币次数超过10次，投币任务终止");
                    break;
                }

                _logger.LogDebug("正在为视频“{title}”投币", video.Item2);

                bool isSuccess = AddCoinsForVideo(video.Item1, 1, _dailyTaskOptions.SelectLike, video.Item2);
                if (isSuccess)
                {
                    successCoins++;
                }

                if (tryCount > 10)
                {
                    _logger.LogInformation("尝试投币次数超过10次，投币任务终止");
                    break;
                }
            }

            _logger.LogInformation("投币任务完成，余额为: {money}", _accountApi.GetCoinBalance().Result.Data.Money);

        }

        /// <summary>
        /// 获取今日的目标投币数
        /// </summary>
        /// <param name="alreadyCoins"></param>
        /// <param name="targetCoins"></param>
        /// <returns></returns>
        private int GetNeedDonateCoinNum(out int alreadyCoins, out int targetCoins)
        {
            int needCoins = 0;

            //获取自定义配置投币数
            int configCoins = _dailyTaskOptions.NumberOfCoins;
            //已投的硬币
            alreadyCoins = _coinDomainService.GetDonatedCoins();
            //目标
            targetCoins = configCoins > Constants.MaxNumberOfDonateCoins
                ? Constants.MaxNumberOfDonateCoins
                : configCoins;

            if (targetCoins > alreadyCoins)
            {
                return targetCoins - alreadyCoins;
            }
            return needCoins;
        }

        /// <summary>
        /// 尝试获取一个没有投币过的视频
        /// </summary>
        /// <returns></returns>
        public Tuple<string, string> TryGetCanDonatedVideo()
        {
            Tuple<string, string> result = null;

            //如果配置upID，则从up中随机尝试获取10次
            if (_dailyTaskOptions.SupportUpIdList.Count > 0)
            {
                result = TryGetCanDonatedVideoByUp(10);
                if (result != null) return result;
            }

            //然后从特别关注列表尝试获取10次
            result = TryGetCanDonatedVideoBySpecialUps(10);
            if (result != null) return result;

            //然后从普通关注列表获取10次
            result = TryGetCanDonatedVideoByFollowingUps(10);
            if (result != null) return result;

            //最后从排行榜尝试5次
            result = TryGetNotDonatedVideoByRegion(5);

            return result;
        }

        /// <summary>
        /// 尝试从配置的up主里随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        public Tuple<string, string> TryGetCanDonatedVideoByUp(int tryCount)
        {
            //是否配置了up主
            if (_dailyTaskOptions.SupportUpIdList.Count == 0) return null;

            return TryGetCanDonateVideoByUps(_dailyTaskOptions.SupportUpIdList, tryCount); ;
        }

        /// <summary>
        /// 尝试从特别关注的Up主中随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        public Tuple<string, string> TryGetCanDonatedVideoBySpecialUps(int tryCount)
        {
            //缓存每个up的视频总数
            Dictionary<long, int> videoCountDic = new Dictionary<long, int>();

            //获取特别关注列表
            BiliApiResponse<List<UpInfo>> specials = _relationApi.GetSpecialFollowings().Result;
            if (specials.Data == null || specials.Data.Count == 0) return null;

            return TryGetCanDonateVideoByUps(specials.Data.Select(x => x.Mid).ToList(), tryCount);
        }

        /// <summary>
        /// 尝试从特别关注的Up主中随机获取一个可以投币的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        public Tuple<string, string> TryGetCanDonatedVideoByFollowingUps(int tryCount)
        {
            //缓存每个up的视频总数
            Dictionary<long, int> videoCountDic = new Dictionary<long, int>();

            //获取特别关注列表
            BiliApiResponse<GetFollowingsResponse> result = _relationApi.GetFollowings(_biliBiliCookieOptions.UserId).Result;
            if (result.Data.Total == 0) return null;

            return TryGetCanDonateVideoByUps(result.Data.List.Select(x => x.Mid).ToList(), tryCount);
        }

        /// <summary>
        /// 尝试从排行榜中获取一个没有看过的视频
        /// </summary>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        public Tuple<string, string> TryGetNotDonatedVideoByRegion(int tryCount)
        {
            if (tryCount <= 0) return null;

            for (int i = 0; i < tryCount; i++)
            {
                Tuple<string, string> video = RegionRanking();
                if (!CanDonatedCoinsForVideo(video.Item1)) continue;
                return video;
            }

            return null;
        }

        /// <summary>
        /// 尝试从指定的up主Id集合中随机获取一个可以投币的视频
        /// </summary>
        /// <param name="upIds"></param>
        /// <param name="tryCount"></param>
        /// <returns></returns>
        private Tuple<string, string> TryGetCanDonateVideoByUps(List<long> upIds, int tryCount)
        {
            //缓存每个up的视频总数
            Dictionary<long, int> videoCountDic = new Dictionary<long, int>();

            //获取特别关注列表
            if (upIds == null || upIds.Count == 0) return null;

            //尝试tryCount次
            for (int i = 0; i < tryCount; i++)
            {
                //获取随机Up主Id
                long randomUpId = upIds[new Random().Next(0, upIds.Count)];
                if (!videoCountDic.TryGetValue(randomUpId, out int videoCount))
                {
                    videoCount = GetVidoeCountOfUp(randomUpId);
                    videoCountDic.Add(randomUpId, videoCount);
                }
                if (videoCount == 0) continue;

                UpVideoInfo videoInfo = GetRandomVideoOfUp(randomUpId, videoCount);
                if (!CanDonatedCoinsForVideo(videoInfo.Aid.ToString())) continue;
                return Tuple.Create(videoInfo.Aid.ToString(), videoInfo.Title);
            }

            return null;
        }

        /// <summary>
        /// 获取随机视频aid
        /// </summary>
        /// <param name="rid">分区id</param>
        /// <param name="day">日榜，三日榜 周榜 1，3，7</param>
        /// <returns>随机返回一个aid</returns>
        private Tuple<string, string> RegionRanking()
        {
            int[] arr = { 1, 3, 4, 5, 160, 22, 119 };
            int rid = arr[new Random().Next(arr.Length - 1)];

            BiliApiResponse<List<RankingInfo>> apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, 3).Result;

            //_logger.LogInformation("获取分区:{rid}的{day}日top10榜单成功", rid, day);
            RankingInfo data = apiResponse.Data[new Random().Next(apiResponse.Data.Count)];

            return Tuple.Create(data.Aid, data.Title);
        }


        /// <summary>
        /// 是否已为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <returns></returns>
        public bool CanDonatedCoinsForVideo(string aid)
        {
            int multiply = _dailyTaskApi.GetDonatedCoinsForVideo(aid).Result.Data.Multiply;
            if (multiply < 2)
            {
                _logger.LogDebug("已为Av" + aid + "投过" + multiply + "枚硬币，可以继续投币");
                return true;
            }
            else
            {
                _logger.LogDebug("已为Av" + aid + " 投过2枚硬币，不能再投币啦");
                return false;
            }
        }

        /// <summary>
        /// 获取UP主的视频总数量
        /// </summary>
        /// <param name="upId"></param>
        /// <returns></returns>
        private int GetVidoeCountOfUp(long upId)
        {
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, 1).Result;
            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.Page.Count;
        }

        /// <summary>
        /// 获取某up的一个随机视频
        /// </summary>
        /// <param name="upId"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        private UpVideoInfo GetRandomVideoOfUp(long upId, int total)
        {
            int pageNum = new Random().Next(1, total + 1);
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, pageNum).Result;

            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.List.Vlist.First();
        }

        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <param name="multiply">投币数量</param>
        /// <param name="select_like">是否同时点赞 1是0否</param>
        /// <returns>是否投币成功</returns>
        public bool AddCoinsForVideo(string aid, int multiply, bool select_like, string title = "")
        {
            BiliApiResponse result = _dailyTaskApi.AddCoinForVideo(aid, multiply, select_like ? 1 : 0, _biliBiliCookieOptions.BiliJct).Result;

            if (result.Code == 0)
            {
                _logger.LogInformation("为“{title}”投币成功", title);
                return true;
            }

            if (result.Code == -111)
            {
                string errorMsg = $"投币异常，Cookie配置项[BiliJct]错误或已过期，请检查并更新。接口返回：{result.Message}";
                _logger.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
            else
            {
                _logger.LogInformation("为“{title}”投币失败，原因：{msg}", title, result.Message);
                return false;
            }
        }

    }
}
