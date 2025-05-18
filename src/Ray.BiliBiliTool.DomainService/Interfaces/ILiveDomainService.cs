using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

/// <summary>
/// 直播中心
/// </summary>
public interface ILiveDomainService : IDomainService
{
    /// <summary>
    /// 签到
    /// </summary>
    Task LiveSign(BiliCookie ck);

    /// <summary>
    /// 银瓜子兑换硬币
    /// </summary>
    /// <returns></returns>
    Task<bool> ExchangeSilver2Coin(BiliCookie ck);

    /// <summary>
    /// 天选抽奖
    /// </summary>
    Task TianXuan(BiliCookie ck);

    Task TryJoinTianXuan(ListItemDto target, BiliCookie ck);

    Task GroupFollowing(BiliCookie ck);

    /// <summary>
    /// 发送弹幕
    /// </summary>
    Task SendDanmakuToFansMedalLive(BiliCookie ck);

    /// <summary>
    /// 直播时长挂机
    /// </summary>
    Task SendHeartBeatToFansMedalLive(BiliCookie ck);

    /// <summary>
    /// 点赞直播间
    /// </summary>
    Task LikeFansMedalLive(BiliCookie ck);
}
