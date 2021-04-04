using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 关注相关接口
    /// </summary>
    [AppendHeader("Host", "api.bilibili.com", AppendHeaderType.AddIfNotExist)]
    [AppendHeader("Referer", "https://space.bilibili.com/", AppendHeaderType.AddIfNotExist)]
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

        /// <summary>
        /// 获取关注分组
        /// </summary>
        /// <returns></returns>
        [AppendHeader("Sec-Fetch-Mode", "no-cors")]
        [AppendHeader("Sec-Fetch-Dest", "script")]
        [HttpGet("/x/relation/tags?jsonp=jsonp")]
        Task<BiliApiResponse<List<TagDto>>> GetTags([AppendHeader("Referer")] string referer = RelationApiConstant.GetTagsReferer);

        /// <summary>
        /// 添加关注分组（tag）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AppendHeader("Origin", "https://space.bilibili.com")]
        [HttpPost("/x/relation/tag/create?cross_domain=true")]
        Task<BiliApiResponse<CreateTagResponse>> CreateTag([FormContent] CreateTagRequest request,
            [AppendHeader("Referer")] string referer = RelationApiConstant.GetTagsReferer);

        /// <summary>
        /// 批量拷贝关注up到某指定分组
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AppendHeader("Origin", "https://space.bilibili.com")]
        [HttpPost("/x/relation/tags/copyUsers")]
        Task<BiliApiResponse> CopyUpsToGroup([FormContent] CopyUserToGroupRequest request,
            [AppendHeader("Referer")] string referer = RelationApiConstant.CopyReferer);
    }

    public class RelationApiConstant
    {
        /// <summary>
        /// GetTags接口中的Referer
        /// {0}为UserId
        /// </summary>
        public const string GetTagsReferer = "https://space.bilibili.com/{0}/fans/follow";

        /// <summary>
        /// CopyUpsToGroup接口中的Referer
        /// {0}为UserId
        /// </summary>
        public const string CopyReferer = "https://space.bilibili.com/{0}/fans/follow?tagid=-1";
    }
}
