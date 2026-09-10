using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;

namespace Ray.BiliBiliTool.DomainService.Dtos;

public class HeartBeatIterationInfoDto(
    long roomId,
    GetLiveRoomInfoResponse roomInfo,
    HeartBeatResponse heartBeatInfo,
    int heartBeatCount,
    long lastBeatTime
)
{
    public long RoomId { get; set; } = roomId;

    public GetLiveRoomInfoResponse RoomInfo { get; set; } = roomInfo;

    public HeartBeatResponse HeartBeatInfo { get; set; } = heartBeatInfo;

    // 成功发送的心跳包个数
    public int HeartBeatCount { get; set; } = heartBeatCount;

    public long LastBeatTime { get; set; } = lastBeatTime;

    // 连续失败的次数
    public int FailedTimes { get; set; }
}
