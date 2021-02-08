using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 充电相关接口
    /// </summary>
    [Header("Host", "api.bilibili.com")]
    public interface IChargeApi : IBiliBiliApi
    {
        /// <summary>
        /// 充电
        /// </summary>
        /// <param name="elec_num">充电电池数量（B币*10）,必须在20-99990之间</param>
        /// <param name="up_mid">充电对象用户UID</param>
        /// <param name="oid">充电来源代码(空间充电：充电对象用户UID;视频充电：稿件avID)</param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [HttpPost("/x/ugcpay/trade/elec/pay/quick?elec_num={elec_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}")]
        [Obsolete]
        Task<BiliApiResponse<ChargeResponse>> Charge(int elec_num, string up_mid, string oid, string csrf);

        /// <summary>
        /// 充电V2
        /// </summary>
        /// <param name="bp_num">B币个数</param>
        /// <param name="up_mid">对方Id</param>
        /// <param name="oid">对方来源代码(空间充电：充电对象用户UID;视频充电：稿件avID)</param>
        /// <param name="csrf">自己的bili_jct</param>
        /// <returns></returns>
        [Header("Content-Type", "application/x-www-form-urlencoded")]
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/ugcpay/web/v2/trade/elec/pay/quick")]
        Task<BiliApiResponse<ChargeV2Response>> ChargeV2([FormContent] ChargeRequest request);

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="elec_num"></param>
        /// <param name="up_mid"></param>
        /// <param name="oid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Header("Content-Type", "application/x-www-form-urlencoded")]
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/ugcpay/trade/elec/message")]
        Task<BiliApiResponse<ChargeResponse>> ChargeComment([FormContent] ChargeCommentRequest request);

    }
}
