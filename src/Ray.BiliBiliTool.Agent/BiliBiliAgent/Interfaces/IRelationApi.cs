using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 关注相关接口
    /// </summary>
    [Header("Host", "api.bilibili.com")]
    [Header("Referer", "https://space.bilibili.com/")]
    public interface IRelationApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取关注列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("/x/relation/followings")]
        Task<BiliApiResponse<GetFollowingsResponse>> GetFollowings(GetFollowingsRequest request);

        /// <summary>
        /// 获取特别关注列表
        /// </summary>
        /// <returns></returns>
        [Header("Cache-Control", "no-cache")]
        [Header("Pragma", "no-cache")]
        [JsonReturn(EnsureMatchAcceptContentType = false)]
        [HttpGet("/x/relation/tag")]
        Task<BiliApiResponse<List<UpInfo>>> GetSpecialFollowings(GetSpecialFollowingsRequest request);
    }
}
