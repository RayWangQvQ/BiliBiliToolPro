using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

/// <summary>
/// 投币
/// </summary>
public interface IDonateCoinDomainService : IDomainService
{
    Task AddCoinsForVideos(BiliCookie ck);

    Task<UpVideoInfo?> TryGetCanDonatedVideo(BiliCookie ck);

    Task<bool> DoAddCoinForVideo(UpVideoInfo video, bool select_like, BiliCookie ck);
}
