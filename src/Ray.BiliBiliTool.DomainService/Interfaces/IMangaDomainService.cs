using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

/// <summary>
/// 漫画
/// </summary>
public interface IMangaDomainService : IDomainService
{
    /// <summary>
    /// 签到
    /// </summary>
    Task MangaSign(BiliCookie ck);

    /// <summary>
    /// 阅读
    /// </summary>
    Task MangaRead(BiliCookie ck);

    /// <summary>
    /// 获取大会员权益
    /// </summary>
    /// <param name="reason_id"></param>
    /// <param name="userIfo"></param>
    Task ReceiveMangaVipReward(int reason_id, UserInfo userIfo, BiliCookie ck);
}
