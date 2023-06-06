using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video;
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
        private readonly IVideoApi _videoApi;
        private readonly IVideoWithoutCookieApi _videoWithoutCookieApi;
        private readonly IWbiDomainService _wbiDomainService;

        public VideoDomainService(
            ILogger<VideoDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliCookie biliBiliCookie,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IOptionsMonitor<Dictionary<string, int>> dicOptions,
            IRelationApi relationApi,
            IVideoApi videoApi,
            IVideoWithoutCookieApi videoWithoutCookieApi,
            IWbiDomainService wbiDomainService
            )
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _relationApi = relationApi;
            _videoApi = videoApi;
            _videoWithoutCookieApi = videoWithoutCookieApi;
            _wbiDomainService = wbiDomainService;
            _biliBiliCookie = biliBiliCookie;
            _expDic = dicOptions.Get(Constants.OptionsNames.ExpDictionaryName);
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
        }

        /// <summary>
        /// 获取视频详情
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        public async Task<VideoDetail> GetVideoDetail(string aid)
        {
            var re = await _videoWithoutCookieApi.GetVideoDetail(aid);
            return re.Data;
        }

        /// <summary>
        /// 从排行榜获取随机视频
        /// </summary>
        /// <returns></returns>
        public async Task<RankingInfo> GetRandomVideoOfRanking()
        {
            var apiResponse = await _videoWithoutCookieApi.GetRegionRankingVideosV2();
            _logger.LogDebug("获取排行榜成功");
            RankingInfo data = apiResponse.Data.List[new Random().Next(apiResponse.Data.List.Count)];
            return data;
        }

        public async Task<UpVideoInfo> GetRandomVideoOfUp(long upId, int total)
        {
            if (total <= 0) return null;

            var req = new SearchVideosByUpIdDto()
            {
                mid = upId,
                ps = 1,
                pn= new Random().Next(1, total + 1)
            };

            var w_ridDto = await _wbiDomainService.GetWridAsync(req);

            var fullDto = new SearchVideosByUpIdFullDto
            {
                mid = upId,
                ps = req.ps,
                pn = req.pn,

                w_rid = w_ridDto.w_rid,
                wts = w_ridDto.wts,
            };

            BiliApiResponse<SearchUpVideosResponse> re = await _videoApi.SearchVideosByUpId(fullDto);

            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.List.Vlist.FirstOrDefault();
        }

        /// <summary>
        /// 获取UP主的视频总数量
        /// </summary>
        /// <param name="upId"></param>
        /// <returns></returns>
        public async Task<int> GetVideoCountOfUp(long upId)
        {
            var req = new SearchVideosByUpIdDto()
            {
                mid = upId
            };

            var w_ridDto = await _wbiDomainService.GetWridAsync(req);

            var fullDto = new SearchVideosByUpIdFullDto
            {
                mid = upId,
                w_rid = w_ridDto.w_rid,
                wts = w_ridDto.wts,
            };

            BiliApiResponse<SearchUpVideosResponse> re = await _videoApi.SearchVideosByUpId(fullDto);
            if (re.Code != 0)
            {
                throw new Exception(re.Message);
            }

            return re.Data.Page.Count;
        }

        public async Task WatchAndShareVideo(DailyTaskInfo dailyTaskStatus)
        {
            VideoInfoDto targetVideo = null;

            //至少有一项未完成，获取视频
            if (!dailyTaskStatus.Watch || !dailyTaskStatus.Share)
            {
                targetVideo = await GetRandomVideoForWatchAndShare();
                _logger.LogInformation("【随机视频】{title}", targetVideo.Title);
            }

            bool watched = false;
            //观看
            if (!dailyTaskStatus.Watch && _dailyTaskOptions.IsWatchVideo)
            {
                await WatchVideo(targetVideo);
                watched = true;
            }
            else
                _logger.LogInformation("今天已经观看过了，不需要再看啦");

            //分享
            if (!dailyTaskStatus.Share && _dailyTaskOptions.IsShareVideo)
            {
                //如果没有打开观看过，则分享前先打开视频
                if (!watched)
                {
                    try
                    {
                        await OpenVideo(targetVideo);
                    }
                    catch (Exception e)
                    {
                        //ignore
                        _logger.LogError("打开视频异常：{msg}", e.Message);
                    }
                }
                await ShareVideo(targetVideo);
            }
            else
                _logger.LogInformation("今天已经分享过了，不用再分享啦");
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        public async Task WatchVideo(VideoInfoDto videoInfo)
        {
            //开始上报一次
            await OpenVideo(videoInfo);

            //结束上报一次
            videoInfo.Duration = videoInfo.Duration ?? 15;
            int max = videoInfo.Duration < 15 ? videoInfo.Duration.Value : 15;
            int playedTime = new Random().Next(1, max);

            var request = new UploadVideoHeartbeatRequest
            {
                Aid = long.Parse(videoInfo.Aid),
                Bvid = videoInfo.Bvid,
                Cid = videoInfo.Cid,
                Mid = long.Parse(_biliBiliCookie.UserId),
                Csrf = _biliBiliCookie.BiliJct,

                Played_time = playedTime,
                Realtime = playedTime,
                Real_played_time = playedTime,
            };
            BiliApiResponse apiResponse = await _videoApi.UploadVideoHeartbeat(request);

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
        /// <param name="videoInfo">视频</param>
        public async Task ShareVideo(VideoInfoDto videoInfo)
        {
            var request = new ShareVideoRequest(long.Parse(videoInfo.Aid), _biliBiliCookie.BiliJct);
            BiliApiResponse apiResponse = await _videoApi.ShareVideo(request);

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

        /// <summary>
        /// 模拟打开视频播放（初始上报一次进度）
        /// </summary>
        /// <param name="videoInfo"></param>
        /// <returns></returns>
        private async Task<bool> OpenVideo(VideoInfoDto videoInfo)
        {
            var request = new UploadVideoHeartbeatRequest
            {
                Aid = long.Parse(videoInfo.Aid),
                Bvid = videoInfo.Bvid,
                Cid = videoInfo.Cid,

                Mid = long.Parse(_biliBiliCookie.UserId),
                Csrf = _biliBiliCookie.BiliJct,
            };

            //开始上报一次
            BiliApiResponse apiResponse = await _videoApi.UploadVideoHeartbeat(request);

            if (apiResponse.Code == 0)
            {
                _logger.LogDebug("打开视频成功");
                return true;
            }
            else
            {
                _logger.LogError("视频打开失败，原因：{msg}", apiResponse.Message);
                return false;
            }
        }

        #region private
        /// <summary>
        /// 获取一个视频用来观看并分享
        /// </summary>
        /// <returns></returns>
        private async Task<VideoInfoDto> GetRandomVideoForWatchAndShare()
        {
            //先从配置的或关注的up中取
            VideoInfoDto video = await GetRandomVideoOfFollowingUps();
            if (video != null) return video;

            //然后从排行榜中取
            RankingInfo t = await GetRandomVideoOfRanking();
            return new VideoInfoDto
            {
                Aid = t.Aid.ToString(),
                Bvid = t.Bvid,
                Cid = t.Cid,
                Copyright = t.Copyright,
                Duration = t.Duration,
                Title = t.Title,
            };
        }

        private async Task<VideoInfoDto> GetRandomVideoOfFollowingUps()
        {
            //配置的UpId
            int configUpsCount = _dailyTaskOptions.SupportUpIdList.Count;
            if (configUpsCount > 0)
            {
                VideoInfoDto video = await GetRandomVideoOfUps(_dailyTaskOptions.SupportUpIdList);
                if (video != null) return video;
            }

            //关注列表
            var request = new GetFollowingsRequest(long.Parse(_biliBiliCookie.UserId));
            BiliApiResponse<GetFollowingsResponse> result = await _relationApi.GetFollowings(request);
            if (result.Data.Total > 0)
            {
                VideoInfoDto video = await GetRandomVideoOfUps(result.Data.List.Select(x => x.Mid).ToList());
                if (video != null) return video;
            }

            return null;
        }

        /// <summary>
        /// 从up集合中获取一个随机视频
        /// </summary>
        /// <param name="upIds"></param>
        /// <returns></returns>
        private async Task<VideoInfoDto> GetRandomVideoOfUps(List<long> upIds)
        {
            long upId = upIds[new Random().Next(0, upIds.Count)];

            if (upId == 0 || upId == long.MinValue) return null;

            int count = await GetVideoCountOfUp(upId);

            if (count > 0)
            {
                UpVideoInfo video = await GetRandomVideoOfUp(upId, count);
                if (video == null) return null;
                return new VideoInfoDto
                {
                    Aid = video.Aid.ToString(),
                    Bvid = video.Bvid,
                    //Cid=,
                    //Copyright=
                    Title = video.Title,
                    Duration = video.Duration
                };
            }

            return null;
        }
        #endregion private
    }
}
