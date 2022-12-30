using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces
{
    [Header("Host", "live-trace.bilibili.com")]
    public interface ILiveTraceApi : IBiliBiliApi
    {

        [HttpGet("/xlive/rdata-interface/v1/heartbeat/webHeartBeat?hb={request}&pf=web")]
        Task<BiliApiResponse<WebHeartBeatResponse>> WebHeartBeat(WebHeartBeatRequest request);

        [Header("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36")]
        [HttpPost("/xlive/data-interface/v1/x25Kn/E")]
        Task<BiliApiResponse<HeartBeatResponse>> EnterRoom([FormContent] EnterRoomRequest request, [FormField] string device);

        [Header("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36")]
        [HttpPost("/xlive/data-interface/v1/x25Kn/X")]
        Task<BiliApiResponse<HeartBeatResponse>> HeartBeat([FormContent] HeartBeatRequest request, [FormField] string device);
    }
}
