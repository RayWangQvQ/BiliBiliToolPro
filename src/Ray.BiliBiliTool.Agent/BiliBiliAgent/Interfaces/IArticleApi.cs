using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    
    [Header("Host", "api.bilibili.com")]
    public interface IArticleApi : IBiliBiliApi
    {
        [Header("Content-Type", "application/x-www-form-urlencoded")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/web-interface/coin/add")]
        Task<BiliApiResponse> AddCoinForArticle([FormContent] AddCoinForArticleRequest request, [Header("referer")] string refer = "https://www.bilibili.com/read/cv5806746/?from=search&spm_id_from=333.337.0.0");


        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://space.bilibili.com")]
        [HttpGet("/x/space/wbi/article")]
        Task<BiliApiResponse<SearchUpArticlesResponse>> SearchUpArticlesByUpId(
            [PathQuery] SearchArticlesByUpIdFullDto request);

        /// <summary>
        /// 获取专栏详情
        /// </summary>
        /// <param name="cvid"></param>
        /// <returns></returns>
        [HttpGet("/x/article/viewinfo?id={cvid}")]
        Task<BiliApiResponse<SearchArticleInfoResponse>> SearchArticleInfo(long cvid);


        [Header("Content-Type", "application/x-www-form-urlencoded")]
        [Header("Referer", "https://www.bilibili.com/read/cv{cvid}/?from=search&spm_id_from=333.337.0.0")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/article/like?id={cvid}&type=1&csrf={csrf}")]
        Task<BiliApiResponse> Like(long cvid, string csrf);

    }

    
}
