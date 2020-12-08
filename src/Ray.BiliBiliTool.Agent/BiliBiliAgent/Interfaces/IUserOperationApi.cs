using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 用户操作API
    /// </summary>
    public interface IUserOperationApi
    {
        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/nav")]
        Task<BiliApiResponse<UseInfo>> LoginByCookie();

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/web-interface/share/add?aid={aid}&csrf={csrf}")]
        Task<BiliApiResponse> ShareVideo(string aid, string csrf);

        /// <summary>
        /// 上传视频观看进度
        /// </summary>
        /// <returns></returns>
        [Post("/x/click-interface/web/heartbeat?aid={aid}&played_time={playedTime}")]
        Task<BiliApiResponse> UploadVideoHeartbeat(string aid, int playedTime);

        /// <summary>
        /// 充电
        /// </summary>
        /// <param name="elec_num"></param>
        /// <param name="up_mid"></param>
        /// <param name="oid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/ugcpay/trade/elec/pay/quick?elec_num={elec_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}")]
        Task<BiliApiResponse<ChargeResponse>> Charge(decimal elec_num, string up_mid, string oid, string csrf);

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

        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="multiply"></param>
        /// <param name="select_like"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/web-interface/coin/add?aid={aid}&multiply={multiply}&select_like={select_like}&cross_domain=true&csrf={csrf}")]
        Task<BiliApiResponse> AddCoinForVideo(string aid, int multiply, int select_like, string csrf);
    }
}
