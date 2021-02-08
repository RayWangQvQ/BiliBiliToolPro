using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// BiliBili每日任务相关接口
    /// </summary>
    [Header("Host", "api.bilibili.com")]
    public interface IDailyTaskApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取每日任务的完成情况
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://account.bilibili.com/account/home")]
        [Header("Origin", "https://account.bilibili.com")]
        [HttpGet("/x/member/web/exp/reward")]
        Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfo();

        /// <summary>
        /// 获取通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpGet("/x/web-interface/coin/today/exp")]
        Task<BiliApiResponse<int>> GetDonateCoinExp();

        /// <summary>
        /// 获取VIP特权
        /// </summary>
        /// <param name="type"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [HttpPost("/x/vip/privilege/receive?type={type}&csrf={csrf}")]
        Task<BiliApiResponse> ReceiveVipPrivilege(int type, string csrf);
    }
}
