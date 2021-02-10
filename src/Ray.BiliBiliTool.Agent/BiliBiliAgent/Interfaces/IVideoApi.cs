using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    /// <summary>
    /// 视频相关接口
    /// </summary>
    [Header("Host", "api.bilibili.com")]
    public interface IVideoApi : IBiliBiliApi
    {
        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/web-interface/share/add")]
        Task<BiliApiResponse> ShareVideo([FormContent] ShareVideoRequest request);

        /// <summary>
        /// 上传视频观看进度
        /// 每15秒上报一次
        /// </summary>
        /// <returns></returns>
        //[Header("Content-Length", "186")]
        [Header("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8")]
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/click-interface/web/heartbeat?aid={aid}&played_time={playedTime}")]
        Task<BiliApiResponse> UploadVideoHeartbeat([FormContent] UploadVideoHeartbeatRequest request);

        #region 投币相关
        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid"></param>
        /// <param name="multiply"></param>
        /// <param name="select_like"></param>
        /// <param name="csrf"></param>
        /// <returns></returns>
        [Header("Content-Type", "application/x-www-form-urlencoded")]
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpPost("/x/web-interface/coin/add?aid={aid}&multiply={multiply}&select_like={select_like}&cross_domain=true&csrf={csrf}")]
        Task<BiliApiResponse> AddCoinForVideo([FormContent] AddCoinRequest request);

        /// <summary>
        /// 获取当前用户对<paramref name="aid"/>视频的投币信息
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        [Header("Referer", "https://www.bilibili.com/")]
        [HttpGet("/x/web-interface/archive/coins")]
        Task<BiliApiResponse<DonatedCoinsForVideo>> GetDonatedCoinsForVideo(GetAlreadyDonatedCoinsRequest request);
        #endregion
    }

    /// <summary>
    /// 不需要传递Cookie的接口
    /// </summary>
    public interface IVideoWithoutCookieApi : IVideoApi
    {
        /// <summary>
        /// 获取视频详情
        /// </summary>
        /// <param name="aid"></param>
        /// <returns></returns>
        [HttpGet("/x/web-interface/view?aid={aid}")]
        Task<BiliApiResponse<VideoDetail>> GetVideoDetail(string aid);

        /// <summary>
        /// 获取某分区下X日内排行榜
        /// </summary>
        /// <param name="rid"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpGet("/x/web-interface/ranking/region?rid={rid}&day={day}")]
        [Obsolete]
        Task<BiliApiResponse<List<RankingInfo>>> GetRegionRankingVideos(int rid, int day);

        /// <summary>
        /// 获取排行榜
        /// </summary>
        /// <returns></returns>
        [Header("Referer", "https://www.bilibili.com/")]
        [Header("Origin", "https://www.bilibili.com")]
        [HttpGet("/x/web-interface/ranking/v2?rid=0&type=all")]
        Task<BiliApiResponse<Ranking>> GetRegionRankingVideosV2();

        /// <summary>
        /// 搜索指定Up的视频列表
        /// </summary>
        /// <param name="upId"></param>
        /// <param name="pageSize">[1,100]验证不通过接口会报异常</param>
        /// <param name="pageNumber"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [Header("Referer", "https://space.bilibili.com/")]
        [Header("Origin", "https://space.bilibili.com")]
        [HttpGet("/x/space/arc/search?mid={upId}&ps={pageSize}&tid=0&pn={pageNumber}&keyword={keyword}&order=pubdate&jsonp=jsonp")]
        Task<BiliApiResponse<SearchUpVideosResponse>> SearchVideosByUpId(long upId, int pageSize = 30, int pageNumber = 1, string keyword = "");

    }
}
