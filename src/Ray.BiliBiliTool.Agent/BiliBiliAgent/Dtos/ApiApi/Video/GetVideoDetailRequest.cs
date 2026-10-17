using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;

/// <summary>
/// 视频详情请求（WBI 签名版）。
/// B站已对 view 接口做 WBI 强制校验，未带 w_rid/wts 的请求会返回
/// code=-412 (request was banned)。此前该接口走非签名路径
/// /x/web-interface/view，在风控升级或请求量较大时会被拦截。
/// 这里改用 /x/web-interface/wbi/view 并携带 wts，使
/// WridEncryptionDelegatingHandler 对其进行签名。
/// </summary>
public class GetVideoDetailRequest : IWrid
{
    public string aid { get; set; } = "";

    public string? w_rid { get; set; }

    public long wts { get; set; }
}
