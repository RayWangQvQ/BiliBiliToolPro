using System;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;
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
    /// 获取任务列表
    /// </summary>
    /// <returns></returns>
    [HttpGet("/x/vip_point/task/combine")]
    Task<BiliApiResponse<VipTaskInfo>> GetTaskListAsync([Header("Cookie")] string ck);

    /// <summary>
    /// 签到任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
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
