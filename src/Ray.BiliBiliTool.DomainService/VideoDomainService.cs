using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
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

        public VideoDomainService(ILogger<VideoDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
        }

        /// <summary>
        /// 获取随机视频
        /// </summary>
        /// <returns></returns>
        public Tuple<string, string> GetRandomVideoOfRegion()
        {
            int[] arr = { 1, 3, 4, 5, 160, 22, 119 };
            int rid = arr[new Random().Next(arr.Length - 1)];

            BiliApiResponse<List<RankingInfo>> apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, 3).Result;
            _logger.LogDebug("获取分区:{rid}的{day}日top10榜单成功", rid, 3);
            RankingInfo data = apiResponse.Data[new Random().Next(apiResponse.Data.Count)];

            return Tuple.Create(data.Aid, data.Title);
        }

        public void WatchAndShareVideo(DailyTaskInfo dailyTaskStatus)
        {
            Tuple<string, string> targetVideo = null;

            if (!dailyTaskStatus.Watch || !dailyTaskStatus.Share)
            {
                targetVideo = GetRandomVideoForWatch();
                _logger.LogInformation("获取随机视频：{title}", targetVideo.Item2);
            }
            if (!dailyTaskStatus.Watch)
                WatchVideo(targetVideo.Item1, targetVideo.Item2);
            else
                _logger.LogInformation("今天已经观看过了，不需要再看啦");

            if (!dailyTaskStatus.Share)
                ShareVideo(targetVideo.Item1, targetVideo.Item2);
            else
                _logger.LogInformation("今天已经分享过了，不要再分享啦");
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        public void WatchVideo(string aid, string title = "")
        {
            int playedTime = new Random().Next(1, 90);
            BiliApiResponse apiResponse = _dailyTaskApi.UploadVideoHeartbeat(aid, playedTime).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("视频播放成功,已观看到第{playedTime}秒", playedTime);
            }
            else
            {
                _logger.LogDebug("视频播放失败,原因：{msg}", apiResponse.Message);
            }
        }

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid">视频aid</param>
        public void ShareVideo(string aid, string title = "")
        {
            BiliApiResponse apiResponse = _dailyTaskApi.ShareVideo(aid, _biliBiliCookieOptions.BiliJct).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("视频分享成功");
            }
            else
            {
                _logger.LogInformation("视频分享失败，原因: {msg}", apiResponse.Message);
                _logger.LogDebug("开发者提示: 如果是csrf校验失败请检查BILI_JCT参数是否正确或者失效");
            }
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

        public List<UpVideoInfo> GetRandomVideosOfUps()
        {
            List<UpVideoInfo> re = new List<UpVideoInfo>();

            int configUpsCount = _dailyTaskOptions.SupportUpIdList.Count;
            if (configUpsCount == 0) return re;

            long upId = _dailyTaskOptions.SupportUpIdList[new Random().Next(0, configUpsCount)];
            int count = GetVidoeCountOfUp(upId);

            int targetNum = 10;
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
            int pageNum = new Random().Next(1, total + 1);
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, pageNum).Result;

            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.List.Vlist.First();
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

        #region private
        private Tuple<string, string> GetRandomVideoForWatch()
        {
            List<UpVideoInfo> list = GetRandomVideosOfUps();
            if (list.Count > 0)
                return Tuple.Create<string, string>(list.First().Aid.ToString(), list.First().Title);

            return GetRandomVideoOfRegion();
        }
        #endregion private
    }
}
