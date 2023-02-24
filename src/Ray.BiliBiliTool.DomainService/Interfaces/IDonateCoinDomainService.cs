using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using System;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 投币
    /// </summary>
    public interface IDonateCoinDomainService : IDomainService
    {
        Task AddCoinsForVideos();

        Task<UpVideoInfo> TryGetCanDonatedVideo();

        Task<bool> DoAddCoinForVideo(UpVideoInfo video, bool select_like);
    }
}
