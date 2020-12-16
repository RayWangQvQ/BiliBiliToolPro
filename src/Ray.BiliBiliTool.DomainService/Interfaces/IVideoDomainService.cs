using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 视频
    /// </summary>
    public interface IVideoDomainService : IDomainService
    {
        /// <summary>
        /// 获取一个随机视频aid
        /// </summary>
        /// <returns></returns>
        Tuple<string, string> GetRandomVideoOfRegion();

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
        void WatchVideo(string aid, string title = "");

        /// <summary>
        /// 分享
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="dailyTaskStatus"></param>
        void ShareVideo(string aid, string title = "");
    }
}
