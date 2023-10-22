using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

public interface IArticleDomainService : IDomainService
{
    // Task<int> GetArticleCountByUp(long upId);
    Task<bool> AddCoinForArticle(long cvid, long mid);

    Task<int> CalculateDonateCoinsCounts();
    Task AddCoinForArticles();
}
