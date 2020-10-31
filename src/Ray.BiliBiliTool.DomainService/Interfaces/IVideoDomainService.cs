using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.Dtos;

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
        string GetRandomVideo();

        /// <summary>
        /// 观看
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="dailyTaskStatus"></param>
        void WatchVideo(string aid, DailyTaskInfo dailyTaskStatus);

        /// <summary>
        /// 分享
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="dailyTaskStatus"></param>
        void ShareVideo(string aid, DailyTaskInfo dailyTaskStatus);

        /// <summary>
        /// 投币
        /// </summary>
        void AddCoinsForVideo();

        /// <summary>
        /// 投币
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="multiply"></param>
        /// <param name="select_like"></param>
        /// <returns></returns>
        bool AddCoinsForVideo(string aid, int multiply, bool select_like);

        /// <summary>
        /// 是否已对某视频投币
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        bool IsDonatedCoinsForVideo(string aid);

        List<UpVideoInfo> GetVideosByUpId(long upId);

        List<UpVideoInfo> GetRandomVideosOfUps();
    }
}
