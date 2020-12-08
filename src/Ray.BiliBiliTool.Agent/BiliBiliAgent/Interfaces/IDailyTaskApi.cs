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
