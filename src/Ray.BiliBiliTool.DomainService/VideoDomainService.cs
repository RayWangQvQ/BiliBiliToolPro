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
using Ray.BiliBiliTool.DomainService.Dtos;
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
        private readonly BiliCookie _biliBiliCookie;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly Dictionary<string, int> _expDic;
        private readonly IRelationApi _relationApi;

        public VideoDomainService(
            ILogger<VideoDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliCookie biliBiliCookie,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IOptionsMonitor<Dictionary<string, int>> dicOptions,
            IRelationApi relationApi
            )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _relationApi = relationApi;
            _biliBiliCookie = biliBiliCookie;
            _expDic = dicOptions.Get(Constants.OptionsNames.ExpDictionaryName);
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

            BiliApiResponse<List<RankingInfo>> apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, 3).GetAwaiter().GetResult();
            _logger.LogDebug("获取分区:{rid}的{day}日top10榜单成功", rid, 3);
            RankingInfo data = apiResponse.Data[new Random().Next(apiResponse.Data.Count)];

            return Tuple.Create(data.Aid, data.Title);
        }

        public UpVideoInfo GetRandomVideoOfUp(long upId, int total)
        {
            int pageNum = new Random().Next(1, total + 1);
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, pageNum).GetAwaiter().GetResult();

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
        public int GetVideoCountOfUp(long upId)
        {
            //todo:通过获取分页实现的，有待改善
            BiliApiResponse<SearchUpVideosResponse> re = _dailyTaskApi.SearchVideosByUpId(upId, 1, 1).GetAwaiter().GetResult();
            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.Page.Count;
        }

        public void WatchAndShareVideo(DailyTaskInfo dailyTaskStatus)
        {
            VideoInfoDto targetVideo = null;

            if (!dailyTaskStatus.Watch || !dailyTaskStatus.Share)
            {
                targetVideo = GetRandomVideoForWatchAndShare();
                _logger.LogInformation("获取随机视频：{title}", targetVideo.Title);
            }

            if (!dailyTaskStatus.Watch)
                WatchVideo(targetVideo);
            else
                _logger.LogInformation("今天已经观看过了，不需要再看啦");

            if (!dailyTaskStatus.Share)
                ShareVideo(targetVideo);
            else
                _logger.LogInformation("今天已经分享过了，不要再分享啦");
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        public void WatchVideo(VideoInfoDto videoInfo)
        {
            int playedTime = new Random().Next(5, videoInfo.SecondsLength ?? 15);
            BiliApiResponse apiResponse = _dailyTaskApi.UploadVideoHeartbeat(videoInfo.Aid, playedTime)
                .GetAwaiter().GetResult();

            if (apiResponse.Code == 0)
            {
                _expDic.TryGetValue("每日观看视频", out int exp);
                _logger.LogInformation("视频播放成功，已观看到第{playedTime}秒，经验+{exp} √", playedTime, exp);
            }
            else
            {
                _logger.LogError("视频播放失败，原因：{msg}", apiResponse.Message);
            }
        }

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid">视频aid</param>
        public void ShareVideo(VideoInfoDto videoInfo)
        {
            BiliApiResponse apiResponse = _dailyTaskApi.ShareVideo(videoInfo.Aid, _biliBiliCookie.BiliJct)
                .GetAwaiter().GetResult();

            if (apiResponse.Code == 0)
            {
                _expDic.TryGetValue("每日观看视频", out int exp);
                _logger.LogInformation("视频分享成功，经验+{exp} √", exp);
            }
            else
            {
                _logger.LogError("视频分享失败，原因: {msg}", apiResponse.Message);
            }
        }

        #region private
        /// <summary>
        /// 获取一个视频用来观看并分享
        /// </summary>
        /// <returns></returns>
        private VideoInfoDto GetRandomVideoForWatchAndShare()
        {
            //先从配置的或关注的up中取
            VideoInfoDto video = GetRandomVideoOfFollowingUps();
            if (video != null) return video;

            //然后从排行榜中取
            Tuple<string, string> t = GetRandomVideoOfRegion();
            return new VideoInfoDto
            {
                Aid = t.Item1,
                Title = t.Item2,
            };
        }

        private VideoInfoDto GetRandomVideoOfFollowingUps()
        {
            //配置的UpId
            int configUpsCount = _dailyTaskOptions.SupportUpIdList.Count;
            if (configUpsCount > 0)
            {
                VideoInfoDto video = GetRandomVideoOfUps(_dailyTaskOptions.SupportUpIdList);
                if (video != null) return video;
            }

            //关注列表
            BiliApiResponse<GetFollowingsResponse> result = _relationApi.GetFollowings(_biliBiliCookie.UserId).GetAwaiter().GetResult();
            if (result.Data.Total > 0)
            {
                VideoInfoDto video = GetRandomVideoOfUps(result.Data.List.Select(x => x.Mid).ToList());
                if (video != null) return video;
            }

            return null;
        }

        /// <summary>
        /// 从up集合中获取一个随机视频
        /// </summary>
        /// <param name="upIds"></param>
        /// <returns></returns>
        private VideoInfoDto GetRandomVideoOfUps(List<long> upIds)
        {
            long upId = upIds[new Random().Next(0, upIds.Count)];

            int count = GetVideoCountOfUp(upId);

            if (count > 0)
            {
                UpVideoInfo video = GetRandomVideoOfUp(upId, count);
                return new VideoInfoDto
                {
                    Aid = video.Aid.ToString(),
                    Title = video.Title,
                    SecondsLength = video.SecondsLength
                };
            }

            return null;
        }
        #endregion private
    }
}
