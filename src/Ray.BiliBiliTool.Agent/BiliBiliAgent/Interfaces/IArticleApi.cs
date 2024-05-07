using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Header("Host", "api.bilibili.com")]
public interface IArticleApi : IBiliBiliApi
{
    [Header("Referer", "https://www.bilibili.com/")]
    [Header("Origin", "https://space.bilibili.com")]
    [HttpGet("/x/space/wbi/article")]
    Task<BiliApiResponse<SearchUpArticlesResponse>> SearchUpArticlesByUpIdAsync([PathQuery] SearchArticlesByUpIdDto request);

    /// <summary>
    /// 获取专栏详情
    /// </summary>
    /// <param name="cvid"></param>
    /// <returns></returns>
    [HttpGet("/x/article/viewinfo?id={cvid}")]
    Task<BiliApiResponse<SearchArticleInfoResponse>> SearchArticleInfoAsync(long cvid);

    /// <summary>
    /// 为专栏文章投币
    /// </summary>
    /// <param name="request"></param>
    /// <param name="refer"></param>
    /// <returns></returns>
    [Header("Content-Type", "application/x-www-form-urlencoded")]
    [Header("Origin", "https://www.bilibili.com")]
    [HttpPost("/x/web-interface/coin/add")]
    Task<BiliApiResponse> AddCoinForArticleAsync([FormContent] AddCoinForArticleRequest request, [Header("referer")] string refer = "https://www.bilibili.com/read/cv5806746/?from=search&spm_id_from=333.337.0.0");

    /// <summary>
    /// 为专栏文章点赞
    /// </summary>
    /// <param name="cvid"></param>
    /// <param name="csrf"></param>
    /// <returns></returns>
    [Header("Content-Type", "application/x-www-form-urlencoded")]
    [Header("Referer", "https://www.bilibili.com/read/cv{cvid}/?from=search&spm_id_from=333.337.0.0")]
    [Header("Origin", "https://www.bilibili.com")]
    [HttpPost("/x/article/like?id={cvid}&type=1&csrf={csrf}")]
    Task<BiliApiResponse> LikeAsync(long cvid, string csrf);
}
