using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 直播中心
    /// </summary>
    public interface ILiveDomainService : IDomainService
    {
        /// <summary>
        /// 签到
        /// </summary>
        Task LiveSign();

        /// <summary>
        /// 银瓜子兑换硬币
        /// </summary>
        /// <returns></returns>
        Task<bool> ExchangeSilver2Coin();

        /// <summary>
        /// 天选抽奖
        /// </summary>
        Task TianXuan();

        Task TryJoinTianXuan(ListItemDto target);

        Task GroupFollowing();

        /// <summary>
        /// 发送弹幕
        /// </summary>
        Task SendDanmakuToFansMedalLive();

        /// <summary>
        /// 直播时长挂机
        /// </summary>
        Task SendHeartBeatToFansMedalLive();

        /// <summary>
        /// 点赞直播间
        /// </summary>
        Task LikeFansMedalLive();
    }
}
