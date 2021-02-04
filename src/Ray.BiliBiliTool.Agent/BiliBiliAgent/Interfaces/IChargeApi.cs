using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 充电相关接口
    /// </summary>
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
        [Post("/x/ugcpay/trade/elec/pay/quick?elec_num={elec_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}")]
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
        [Post("/x/ugcpay/web/v2/trade/elec/pay/quick?is_bp_remains_prior=true&bp_num={bp_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}")]
        Task<BiliApiResponse<ChargeV2Response>> ChargeV2(decimal bp_num, string up_mid, string oid, string csrf);

        /// <summary>
        /// 充电后留言
        /// </summary>
        /// <param name="elec_num"></param>
        /// <param name="up_mid"></param>
        /// <param name="oid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/ugcpay/trade/elec/message?order_id={order_id}&message={message}&csrf={csrf}")]
        Task<BiliApiResponse<ChargeResponse>> ChargeComment(string order_id, string message, string csrf);

    }
}
