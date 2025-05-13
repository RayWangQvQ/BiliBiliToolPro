using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.DomainService.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces;

/// <summary>
/// 视频
/// </summary>
public interface IVideoDomainService : IDomainService
{
    /// <summary>
    /// 获取视频详情
    /// </summary>
    /// <param name="aid"></param>
    /// <returns></returns>
    Task<VideoDetail> GetVideoDetail(string aid);

    /// <summary>
    /// 从排行榜获取一个随机视频
    /// </summary>
    /// <returns></returns>
    Task<RankingInfo> GetRandomVideoOfRanking();

    /// <summary>
    /// 从某个指定UP下获取随机视频
    /// </summary>
    /// <param name="upId"></param>
    /// <param name="total"></param>
    /// <returns></returns>
    Task<UpVideoInfo> GetRandomVideoOfUp(long upId, int total, BiliCookie ck);

    Task<int> GetVideoCountOfUp(long upId, BiliCookie ck);

    /// <summary>
    /// 观看并分享视频
    /// </summary>
    /// <param name="dailyTaskStatus"></param>
    Task WatchAndShareVideo(DailyTaskInfo dailyTaskStatus, BiliCookie ck);

    /// <summary>
    /// 观看
    /// </summary>
    /// <param name="aid"></param>
    /// <param name="dailyTaskStatus"></param>
    Task WatchVideo(VideoInfoDto videoInfo, BiliCookie ck);

    /// <summary>
    /// 分享
    /// </summary>
    /// <param name="aid"></param>
    /// <param name="dailyTaskStatus"></param>
    Task ShareVideo(VideoInfoDto videoInfo, BiliCookie ck);
}
