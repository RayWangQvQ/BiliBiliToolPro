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
    Task<BiliApiResponse<VipTaskInfo>> GetTaskListAsync();

    /// <summary>
    /// 签到任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/score/task/sign")]
    Task<BiliApiResponse> SignAsync([FormContent] SignRequest request);

    /// <summary>
    /// 领取任务
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/pgc/activity/score/task/receive")]
    Task<BiliApiResponse> Receive([JsonContent] ReceiveOrCompleteTaskRequest request);

    [HttpPost("/pgc/activity/score/task/receive/v2")]
    Task<BiliApiResponse> ReceiveV2([FormContent] ReceiveOrCompleteTaskRequest request);


    [HttpPost("/pgc/activity/score/task/complete")]
    Task<BiliApiResponse> CompleteAsync([JsonContent] ReceiveOrCompleteTaskRequest request);

    [HttpPost("/pgc/activity/score/task/complete/v2")]
    Task<BiliApiResponse> CompleteV2([FormContent] ReceiveOrCompleteTaskRequest request);


    [HttpPost("/pgc/activity/deliver/task/complete")]
    Task<BiliApiResponse> ViewComplete([FormContent] ViewRequest request);

    [HttpGet("/x/vip/privilege/my")]
    Task<BiliApiResponse<VouchersInfoResponse>> GetVouchersInfoAsync();

    /// <summary>
    /// 兑换大会员经验
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/x/vip/experience/add")]
    Task<BiliApiResponse> ObtainVipExperienceAsync([FormContent] VipExperienceRequest request);
}
