using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Refit;

namespace BiliBiliTool.Agent.Interfaces
{
    /// <summary>
    /// BiliBili每日任务相关接口
    /// </summary>
    [Headers(
        "Accept:application/json, text/plain, */*",
        "Referer:https://www.bilibili.com/",
        "Connection:keep-alive",
        "User-Agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36 Edg/85.0.564.70"
        )]
    public interface IDailyTaskApi
    {
        [Get("/x/web-interface/nav")]
        Task<BiliApiResponse<LoginResponse>> LoginByCookie();

        /// <summary>
        /// 获取每日任务的完成情况
        /// </summary>
        /// <returns></returns>
        [Get("/x/member/web/exp/reward")]
        Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfo();

        /// <summary>
        /// 获取某分区下X日内排行榜
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        [Get("/x/web-interface/ranking/region?rid={rid}&day={day}")]
        Task<BiliApiResponse<List<RankingInfo>>> GetRegionRankingVideos(int rid, int day);
    }
}
