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
        string GetRandomVideo();

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
        bool AddCoinsForVideo(string aid, int multiply, bool select_like, string title = "");

        /// <summary>
        /// 是否已对某视频投币
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        bool CanDonatedCoinsForVideo(string aid);

        List<UpVideoInfo> GetRandomVideosOfUps();

        Tuple<string, string> TryGetCanDonatedVideo();
    }
}
