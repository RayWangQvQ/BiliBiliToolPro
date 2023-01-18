using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.DomainService.Dtos
{
    public class FansMedalInfoDto
    {
        public FansMedalInfoDto(int roomId, MedalWallDto medalInfo, GetLiveRoomInfoResponse liveRoomInfo)
        {
            this.RoomId = roomId;
            this.MedalInfo = medalInfo;
            this.LiveRoomInfo = liveRoomInfo;
        }

        // 直播间 id
        public int RoomId { get; set; }

        // 粉丝牌信息
        public MedalWallDto MedalInfo { get; set; }

        // 直播间信息
        public GetLiveRoomInfoResponse LiveRoomInfo { get; set; }
    }
}
