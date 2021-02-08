using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.DomainService.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 视频
    /// </summary>
    public interface IVideoDomainService : IDomainService
    {
        /// <summary>
        /// 获取视频详情
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        VideoDetail GetVideoDetail(string aid);

        /// <summary>
        /// 从排行榜获取一个随机视频
        /// </summary>
        /// <returns></returns>
        RankingInfo GetRandomVideoOfRanking();

        /// <summary>
        /// 从某个指定UP下获取随机视频
        /// </summary>
        /// <param name="upId"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        UpVideoInfo GetRandomVideoOfUp(long upId, int total);

        int GetVideoCountOfUp(long upId);

        /// <summary>
        /// 观看并分享视频
        /// </summary>
        /// <param name="dailyTaskStatus"></param>
        void WatchAndShareVideo(DailyTaskInfo dailyTaskStatus);

        /// <summary>
        /// 观看
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="dailyTaskStatus"></param>
        void WatchVideo(VideoInfoDto videoInfo);

        /// <summary>
        /// 分享
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="dailyTaskStatus"></param>
        void ShareVideo(VideoInfoDto videoInfo);
    }
}
