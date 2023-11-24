using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

public interface IArticleDomainService : IDomainService
{
    Task<bool> AddCoinForArticle(long cvid, long mid);

    Task<bool> AddCoinForArticles();

    Task LikeArticle(long cvid);
}
