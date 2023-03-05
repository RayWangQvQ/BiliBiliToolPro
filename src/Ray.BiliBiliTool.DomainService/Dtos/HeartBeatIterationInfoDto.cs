using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Dtos
{
    public class HeartBeatIterationInfoDto
    {
        public HeartBeatIterationInfoDto(
            long roomId,
            GetLiveRoomInfoResponse roomInfo,
            HeartBeatResponse heartBeatInfo,
            int heartBeatCount,
            long lastBeatTime)
        {
            RoomId = roomId;
            RoomInfo = roomInfo;
            HeartBeatInfo = heartBeatInfo;
            HeartBeatCount = heartBeatCount;
            LastBeatTime = lastBeatTime;
        }

        public long RoomId { get; set; } = 0;

        public GetLiveRoomInfoResponse RoomInfo { get; set; } = new();

        public HeartBeatResponse HeartBeatInfo { get; set; } = new();

        // 成功发送的心跳包个数
        public int HeartBeatCount { get; set; } = 0;

        public long LastBeatTime { get; set; } = 0;

        // 连续失败的次数
        public int FailedTimes { get; set; } = 0;
    }
}
