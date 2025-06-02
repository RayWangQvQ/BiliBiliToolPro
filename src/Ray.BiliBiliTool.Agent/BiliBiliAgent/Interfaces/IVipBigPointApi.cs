using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask.ThreeDaysSign;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 大会员大积分
/// </summary>
[Header("Host", "api.bilibili.com")]
[Header("Referer", "https://big.bilibili.com/mobile/bigPoint/task")]
[LogFilter]
public interface IVipBigPointApi
{
    /// <summary>
    /// 获取签到信息
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    [HttpGet("/x/vip/vip_center/sign_in/three_days_sign")]
    Task<BiliApiResponse<ThreeDaySignResponse>> GetThreeDaySignAsync(
        [PathQuery] ThreeDaySignRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 获取任务列表
    /// </summary>
    /// <remarks>里面的登录信息是错误的，阿B特色</remarks>
    /// <returns></returns>
    [Obsolete("Using IMallApi.GetCombineAsync instead.")]
    [HttpGet("/x/vip_point/task/combine")]
    Task<BiliApiResponse<VipBigPointCombine>> GetCombineAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 签到任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Obsolete("Using IMallApi.Sign2Async instead.")]
    [HttpPost("/pgc/activity/score/task/sign")]
    Task<BiliApiResponse> SignAsync(
        [FormContent] SignRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 领取任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Obsolete]
    [HttpPost("/pgc/activity/score/task/receive")]
    Task<BiliApiResponse> Receive(
        [JsonContent] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 领取任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/score/task/receive/v2")]
    Task<BiliApiResponse> ReceiveV2(
        [FormContent] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/score/task/complete")]
    Task<BiliApiResponse> CompleteAsync(
        [JsonContent] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/score/task/complete/v2")]
    Task<BiliApiResponse> CompleteV2(
        [FormContent] ReceiveOrCompleteTaskRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成浏览页面任务
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/deliver/task/complete")]
    Task<BiliApiResponse> ViewComplete(
        [FormContent] ViewRequest request,
        [Header("Cookie")] string ck
    );

    [HttpGet("/x/vip/privilege/my")]
    Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfoAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 兑换大会员经验
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/x/vip/experience/add")]
    Task<BiliApiResponse> ObtainVipExperienceAsync(
        [FormContent] VipExperienceRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 开始观看剧集任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<BiliApiResponse<StartOgvWatchResponse>> StartOgvWatchAsync(
        StartOgvWatchRequest request,
        [Header("Cookie")] string ck
    );

    /// <summary>
    /// 完成观看剧集任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<BiliApiResponse> CompleteOgvWatchAsync(
        CompleteOgvWatchRequest request,
        [Header("Cookie")] string ck
    );
}
