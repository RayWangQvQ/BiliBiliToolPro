using System;
using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

public class WebHeartBeatRequest
{
    public WebHeartBeatRequest(int room_id, int next_interval)
    {
        this.RoomId = room_id;
        this.NextInterval = next_interval;
    }

    public long RoomId { set; get; }

    public int NextInterval { set; get; }

    public override string ToString()
    {
        string arg = $"{this.NextInterval}|{this.RoomId}|1|0";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(arg));
    }
}