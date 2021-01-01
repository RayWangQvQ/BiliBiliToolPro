using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// BiliBili每日任务相关接口
    /// </summary>
    public interface IDailyTaskApi : IBiliBiliApi, IUserInfoApi, IUserOperationApi
    {
        //todo：考虑根据领域拆分该接口

        /// <summary>
        /// 获取某分区下X日内排行榜
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        [Get("/x/web-interface/ranking/region?rid={rid}&day={day}")]
        Task<BiliApiResponse<List<RankingInfo>>> GetRegionRankingVideos(int rid, int day);

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

        #region 用户信息

        /// <summary>
        /// 获取每日任务的完成情况
        /// </summary>
        /// <returns></returns>
        [Get("/x/member/web/exp/reward")]
        Task<BiliApiResponse<DailyTaskInfo>> GetDailyTaskRewardInfo();

        /// <summary>
        /// 获取通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/coin/today/exp")]
        Task<BiliApiResponse<int>> GetDonateCoinExp();

        /// <summary>
        /// 获取VIP特权
        /// </summary>
        /// <param name="type"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/vip/privilege/receive?type={type}&csrf={csrf}")]
        Task<BiliApiResponse> ReceiveVipPrivilege(int type, string csrf);

        /// <summary>
        /// 获取当前用户对<paramref name="aid"/>视频的投币信息
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        [Get("/x/web-interface/archive/coins?aid={aid}")]
        Task<BiliApiResponse<DonatedCoinsForVideo>> GetDonatedCoinsForVideo(string aid);

        #endregion 用户信息

        #region 用户操作

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

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        [Get("/x/web-interface/nav")]
        Task<BiliApiResponse<UserInfo>> LoginByCookie();

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
        /// <param name="elec_num">充电电池数量（B币*10）,必须在20-99990之间</param>
        /// <param name="up_mid">充电对象用户UID</param>
        /// <param name="oid">充电来源代码(空间充电：充电对象用户UID;视频充电：稿件avID)</param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Post("/x/ugcpay/trade/elec/pay/quick?elec_num={elec_num}&up_mid={up_mid}&otype=up&oid={oid}&csrf={csrf}")]
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

        #endregion 用户操作
    }
}
