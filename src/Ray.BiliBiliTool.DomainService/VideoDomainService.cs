using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 视频
    /// </summary>
    public class VideoDomainService : IVideoDomainService
    {
        private readonly ILogger<VideoDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookieOptions _biliBiliCookieOptions;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly IAccountApi _accountApi;
        private readonly ICoinDomainService _coinDomainService;

        public VideoDomainService(ILogger<VideoDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IAccountApi accountApi,
            ICoinDomainService coinDomainService)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _accountApi = accountApi;
            _coinDomainService = coinDomainService;
        }

        /// <summary>
        /// 获取随机视频
        /// </summary>
        /// <returns></returns>
        public string GetRandomVideo()
        {
            return RegionRanking().Item1;
        }

        public void WatchAndShareVideo(DailyTaskInfo dailyTaskStatus)
        {
            var targetVideo = GetRandomVideoForWatch();

            WatchVideo(dailyTaskStatus, targetVideo.Item1, targetVideo.Item2);
            ShareVideo(dailyTaskStatus, targetVideo.Item1, targetVideo.Item2);
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        public void WatchVideo(DailyTaskInfo dailyTaskStatus, string aid, string title = "")
        {
            if (dailyTaskStatus.Watch)
            {
                _logger.LogInformation("本日观看视频任务已经完成了，不需要再观看视频了");
                return;
            }

            int playedTime = new Random().Next(1, 90);
            var apiResponse = _dailyTaskApi.UploadVideoHeartbeat(aid, playedTime).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("av{aid}({title})播放成功,已观看到第{playedTime}秒", aid, title, playedTime);
            }
            else
            {
                _logger.LogDebug("av{aid}({title})播放失败,原因：{msg}", aid, title, apiResponse.Message);
            }
        }

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid">视频aid</param>
        public void ShareVideo(DailyTaskInfo dailyTaskStatus, string aid, string title = "")
        {
            if (dailyTaskStatus.Share)
            {
                _logger.LogInformation("本日分享视频任务已经完成了，不需要再分享视频了");
                return;
            }

            var apiResponse = _dailyTaskApi.ShareVideo(aid, _biliBiliCookieOptions.BiliJct).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("视频: av{aid}({title})分享成功", aid, title);
            }
            else
            {
                _logger.LogDebug("视频分享失败，原因: {msg}", apiResponse.Message);
                _logger.LogDebug("开发者提示: 如果是csrf校验失败请检查BILI_JCT参数是否正确或者失效");
            }
        }

        /// <summary>
        /// 是否已为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <returns></returns>
        public bool IsDonatedCoinsForVideo(string aid)
        {
            int multiply = _dailyTaskApi.GetDonatedCoinsForVideo(aid).Result.Data.Multiply;
            if (multiply > 0)
            {
                //_logger.LogInformation("已经为Av" + aid + "投过" + multiply + "枚硬币啦");
                return true;
            }
            else
            {
                //_logger.LogInformation("还没有为Av" + aid + " 投过硬币，开始投币");
                return false;
            }
        }

        /// <summary>
        /// 投币
        /// </summary>
        public void AddCoinsForVideo()
        {
            int needCoins = GetNeedDonateCoins(out int alreadyCoins, out int targetCoins);
            _logger.LogInformation("今日已投{already}枚硬币，目标是投{target}枚硬币", alreadyCoins, targetCoins);

            if (needCoins <= 0)
            {
                _logger.LogInformation("已完成投币任务，今天不需要再投啦");
                return;
            }

            _logger.LogInformation("还需再投{need}枚硬币", needCoins);

            //投币前硬币余额
            var coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("投币前余额为 : " + coinBalance);

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
            int tryCount = 0;//投币最多操作数 解决csrf校验失败时死循环的问题
            var upVideos = GetRandomVideosOfUps();
            int upVideoIndex = 0;
            while (successCoins < needCoins)
            {
                tryCount++;

                string aid;
                string title;
                //优先使用配置的up主视频
                if (upVideoIndex < upVideos.Count)
                {
                    aid = upVideos[tryCount - 1].Aid.ToString();
                    title = upVideos[tryCount - 1].Title;
                    upVideoIndex++;
                }
                else
                {
                    var re = RegionRanking();
                    aid = re.Item1;
                    title = re.Item2;
                }

                _logger.LogDebug("正在为av{aid}({title})投币", aid, title);

                //判断曾经是否对此av投币过
                if (IsDonatedCoinsForVideo(aid))
                {
                    _logger.LogDebug("{aid}({title})已经投币过了", aid, title);
                    tryCount--;
                    continue;
                }

                bool isSuccess = AddCoinsForVideo(aid, 1, _dailyTaskOptions.SelectLike, title);
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

            _logger.LogInformation("投币任务完成后余额为: " + _accountApi.GetCoinBalance().Result.Data.Money);
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
            var result = _dailyTaskApi.AddCoinForVideo(aid, multiply, select_like ? 1 : 0, _biliBiliCookieOptions.BiliJct).Result;

            if (result.Code == 0)
            {
                _logger.LogInformation("为Av{aid}({title})投币成功", aid, title);
                return true;
            }
            else
            {
                _logger.LogDebug("为Av{aid}({title})投币失败，原因：{msg}", aid, title, result.Message);
                return false;
            }
        }

        public List<UpVideoInfo> GetVideosByUpId(long upId)
        {
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, 11).Result;

            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.List.Vlist;
        }

        public List<UpVideoInfo> GetRandomVideosOfUps()
        {
            var re = new List<UpVideoInfo>();

            int configUpsCount = _dailyTaskOptions.SupportUpIdList.Count;
            if (configUpsCount == 0) return re;

            long upId = _dailyTaskOptions.SupportUpIdList[new Random().Next(0, configUpsCount)];
            int count = GetVidoeCountOfUp(upId);

            var targetNum = 10;
            if (count < 10) targetNum = count;
            for (int i = 0; i < targetNum; i++)
            {
                UpVideoInfo videoInfo = GetRandomVideoOfUp(upId, count);
                if (re.Count(x => x.Aid == videoInfo.Aid) == 0) re.Add(videoInfo);
            }

            return re;
        }

        private UpVideoInfo GetRandomVideoOfUp(long upId, int total)
        {
            var pageNum = new Random().Next(1, total + 1);
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, pageNum).Result;

            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.List.Vlist.First();
        }

        private int GetVidoeCountOfUp(long upId)
        {
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, 1).Result;
            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.Page.Count;
        }


        #region private

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

            var apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, 3).Result;

            //_logger.LogInformation("获取分区:{rid}的{day}日top10榜单成功", rid, day);
            var data = apiResponse.Data[new Random().Next(apiResponse.Data.Count)];

            return Tuple.Create<string, string>(data.Aid, data.Title);
        }

        /// <summary>
        /// 获取今日的目标投币数
        /// </summary>
        /// <param name="alreadyCoins"></param>
        /// <param name="targetCoins"></param>
        /// <returns></returns>
        private int GetNeedDonateCoins(out int alreadyCoins, out int targetCoins)
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

        private Tuple<string, string> GetRandomVideoForWatch()
        {
            List<UpVideoInfo> list = GetRandomVideosOfUps();
            if (list.Count > 0)
                return Tuple.Create<string, string>(list.First().Aid.ToString(), list.First().Title);

            return RegionRanking();
        }
        #endregion
    }
}
