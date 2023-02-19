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

        Task<Tuple<string, string>> TryGetCanDonatedVideo();

        Task<bool> DoAddCoinForVideo(string aid, int multiply, bool select_like, string title = "");
    }
}
