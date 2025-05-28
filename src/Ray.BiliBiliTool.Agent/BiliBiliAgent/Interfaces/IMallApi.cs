using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

/// <summary>
/// 大会员大积分
/// </summary>
[LogFilter]
[Header("Host", "api.bilibili.com")]
public interface IMallApi
{
    /// <summary>
    /// 签到任务
    /// </summary>
    /// <param name="requestPath"></param>
    /// <param name="request"></param>
    /// <param name="ck"></param>
    /// <returns></returns>
    [Header("Referer", "https://big.bilibili.com/mobile/index")]
    [HttpPost("/pgc/activity/score/task/sign2")]
    Task<BiliApiResponse> Sign2Async(
        [PathQuery] Sign2RequestPath requestPath,
        [JsonContent] Sign2Request request,
        [Header("Cookie")] string ck
    );
}
