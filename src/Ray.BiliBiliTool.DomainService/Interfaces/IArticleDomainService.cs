using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

public interface IArticleDomainService : IDomainService
{
    Task<bool> AddCoinForArticle(long cvid, long mid, BiliCookie ck);

    Task<bool> AddCoinForArticles(BiliCookie ck);

    Task LikeArticle(long cvid, BiliCookie ck);
}
