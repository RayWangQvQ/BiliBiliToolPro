using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Refit;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

[Headers("Host: live-trace.bilibili.com")]
public interface ILiveTraceApi
{
    [Get("/xlive/rdata-interface/v1/heartbeat/webHeartBeat?hb={request}&pf=web")]
    Task<BiliApiResponse<WebHeartBeatResponse>> WebHeartBeat(
        WebHeartBeatRequest request,
        [Header("Cookie")] string ck
    );

    [Post("/xlive/data-interface/v1/x25Kn/E")]
    Task<BiliApiResponse<HeartBeatResponse>> EnterRoom(
        [Body(BodySerializationMethod.UrlEncoded)] EnterRoomRequest request,
        [Header("Cookie")] string ck
    );

    [Post("/xlive/data-interface/v1/x25Kn/X")]
    Task<BiliApiResponse<HeartBeatResponse>> HeartBeat(
        [Body(BodySerializationMethod.UrlEncoded)] HeartBeatRequest request,
        [Header("Cookie")] string ck
    );
}
