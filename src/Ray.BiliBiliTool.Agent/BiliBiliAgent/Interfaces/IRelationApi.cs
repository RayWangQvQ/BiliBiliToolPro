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
    public interface IRelationApi : IBiliBiliApi
    {
        /// <summary>
        /// 获取关注列表
        /// </summary>
        /// <param name="vmid"></param>
        /// <param name="pn"></param>
        /// <param name="ps"></param>
        /// <param name="order"></param>
        /// <param name="order_type"></param>
        /// <returns></returns>
        [HttpGet("/followings?vmid={vmid}&pn={pn}&ps={ps}&order={order}&order_type={order_type}")]
        Task<BiliApiResponse<GetFollowingsResponse>> GetFollowings(string vmid, int pn = 1, int ps = 50, string order = "desc", string order_type = "attention");

        /// <summary>
        /// 获取特别关注列表
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        [HttpGet("/tag?tagid=-10&pn={pn}&ps={ps}")]
        Task<BiliApiResponse<List<UpInfo>>> GetSpecialFollowings(int pn = 1, int ps = 50);
    }
}
