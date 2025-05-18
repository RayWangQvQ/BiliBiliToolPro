using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Header("Host", "live-trace.bilibili.com")]
public interface ILiveTraceApi : IBiliBiliApi
{
    [HttpGet("/xlive/rdata-interface/v1/heartbeat/webHeartBeat?hb={request}&pf=web")]
    Task<BiliApiResponse<WebHeartBeatResponse>> WebHeartBeat(
        WebHeartBeatRequest request,
        [Header("Cookie")] string ck
    );

    [HttpPost("/xlive/data-interface/v1/x25Kn/E")]
    Task<BiliApiResponse<HeartBeatResponse>> EnterRoom(
        [FormContent] EnterRoomRequest request,
        [Header("Cookie")] string ck
    );

    [HttpPost("/xlive/data-interface/v1/x25Kn/X")]
    Task<BiliApiResponse<HeartBeatResponse>> HeartBeat(
        [FormContent] HeartBeatRequest request,
        [Header("Cookie")] string ck
    );
}
