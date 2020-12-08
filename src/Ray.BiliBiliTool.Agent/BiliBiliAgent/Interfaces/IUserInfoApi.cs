using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 用户信息接口API
    /// </summary>
    public interface IUserInfoApi
    {
        /// <summary>
        /// 获取每日任务的完成情况
        /// </summary>
        /// <returns></returns>
        [Get("/x/member/web/exp/reward")]
        Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfo();

        /// <summary>
        /// 获取通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/coin/today/exp")]
        Task<BiliApiResponse<int>> GetDonateCoinExp();

        /// <summary>
        /// 获取VIP特权
        /// </summary>
        /// <param name="type"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/vip/privilege/receive?type={type}&csrf={csrf}")]
        Task<BiliApiResponse> ReceiveVipPrivilege(int type, string csrf);

        /// <summary>
        /// 获取当前用户对<paramref name="aid"/>视频的投币信息
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        [Get("/x/web-interface/archive/coins?aid={aid}")]
        Task<BiliApiResponse<DonatedCoinsForVideo>> GetDonatedCoinsForVideo(string aid);
    }
}
