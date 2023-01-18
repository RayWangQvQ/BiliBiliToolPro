using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Config.Options;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Host", "live-trace.bilibili.com")]
    public interface ILiveTraceApi : IBiliBiliApi
    {

        [HttpGet("/xlive/rdata-interface/v1/heartbeat/webHeartBeat?hb={request}&pf=web")]
        Task<BiliApiResponse<WebHeartBeatResponse>> WebHeartBeat(WebHeartBeatRequest request);

        /*
            单独引入 device 参数的原因：
            WebApiClientCore 库的 FormContent 有个已知 issue https://github.com/dotnetcore/WebApiClient/issues/211
            会将表单中的双引号自动加入反斜杠转义...
            如 ["key":"value"] => [\"key\":\"value\"]
        */
        [HttpPost("/xlive/data-interface/v1/x25Kn/E")]
        Task<BiliApiResponse<HeartBeatResponse>> EnterRoom([FormContent] EnterRoomRequest request, [FormField] string device);

        [HttpPost("/xlive/data-interface/v1/x25Kn/X")]
        Task<BiliApiResponse<HeartBeatResponse>> HeartBeat([FormContent] HeartBeatRequest request, [FormField] string device);
    }
}
