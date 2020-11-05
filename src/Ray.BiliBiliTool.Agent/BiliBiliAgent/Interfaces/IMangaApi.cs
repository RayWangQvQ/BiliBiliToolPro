using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 漫画接口
    /// </summary>
    public interface IMangaApi : IBiliBiliApi
    {
        /// <summary>
        /// 漫画签到
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        [Post("/twirp/activity.v1.Activity/ClockIn?platform={platform}")]
        Task<BiliApiResponse> ClockIn(string platform);


        /// <summary>
        /// 获取会员漫画奖励
        /// </summary>
        /// <param name="reason_id"></param>
        /// <returns></returns>
        [Post("/twirp/user.v1.User/GetVipReward?reason_id={reason_id}")]
        Task<BiliApiResponse<MangaVipRewardResponse>> ReceiveMangaVipReward(int reason_id);

    }
}
