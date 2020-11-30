using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// BiliBili每日任务相关接口
    /// </summary>
    public interface IDailyTaskApi : IBiliBiliApi
    {
        //todo：考虑根据领域拆分该接口

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [Headers("Referer")]//需要移除
        [Get("/x/web-interface/nav")]
        Task<BiliApiResponse<UseInfo>> LoginByCookie();

        /// <summary>
        /// 获取每日任务的完成情况
        /// </summary>
        /// <returns></returns>
        [Get("/x/member/web/exp/reward")]
        Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfo();

        /// <summary>
        /// 获取某分区下X日内排行榜
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        [Get("/x/web-interface/ranking/region?rid={rid}&day={day}")]
        Task<BiliApiResponse<List<RankingInfo>>> GetRegionRankingVideos(int rid, int day);

        /// <summary>
        /// 上传视频观看进度
        /// </summary>
        /// <returns></returns>
        [Post("/x/click-interface/web/heartbeat?aid={aid}&played_time={playedTime}")]
        Task<BiliApiResponse> UploadVideoHeartbeat(string aid, int playedTime);

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/web-interface/share/add?aid={aid}&csrf={csrf}")]
        Task<BiliApiResponse> ShareVideo(string aid, string csrf);

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        [Get("/x/web-interface/archive/coins?aid={aid}")]
        Task<BiliApiResponse<DonatedCoinsForVideo>> GetDonatedCoinsForVideo(string aid);

        [Post("/x/web-interface/coin/add?aid={aid}&multiply={multiply}&select_like={select_like}&cross_domain=true&csrf={csrf}")]
        Task<BiliApiResponse> AddCoinForVideo(string aid, int multiply, int select_like, string csrf);

        [Post("/x/vip/privilege/receive?type={type}&csrf={csrf}")]
        Task<BiliApiResponse> ReceiveVipPrivilege(int type, string csrf);

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
        /// 充电
        /// </summary>
        /// <param name="elec_num"></param>
        /// <param name="up_mid"></param>
        /// <param name="oid"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/ugcpay/trade/elec/message?order_id={order_id}&message={message}&csrf={csrf}")]
        Task<BiliApiResponse<ChargeResponse>> ChargeComment(string order_id, string message, string csrf);

        /// <summary>
        /// 获取指定Up的视频
        /// </summary>
        /// <param name="upId"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Get("/x/v2/medialist/resource/list?type=1&biz_id={upId}&bvid=&mobi_app=web&ps={pageSize}&direction=false")]
        Task<BiliApiResponse<GetVideosResponse>> GetVideosByUpId(long upId, int pageSize);

        /// <summary>
        /// 获取指定Up的视频
        /// </summary>
        /// <param name="upId"></param>
        /// <param name="pageSize">[1,100]验证不通过接口会报异常</param>
        /// <param name="pageNumber"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [Get("/x/space/arc/search?mid={upId}&ps={pageSize}&tid=0&pn={pageNumber}&keyword={keyword}&order=pubdate&jsonp=jsonp")]
        Task<BiliApiResponse<SearchUpVideosResponse>> SearchVideosByUpId(long upId, int pageSize = 20, int pageNumber = 1, string keyword = "");

        /// <summary>
        /// 获取通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/coin/today/exp")]
        Task<BiliApiResponse<int>> GetDonateCoinExp();
    }
}
