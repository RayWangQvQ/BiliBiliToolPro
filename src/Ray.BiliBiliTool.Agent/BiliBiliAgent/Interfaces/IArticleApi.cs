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

    }

    
}
