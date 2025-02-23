using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

public class HeartBeatResponse
{
    public int Heartbeat_interval { get; set; }

    public string Secret_key { get; set; }

    public List<int> Secret_rule { get; set;}

    public long Timestamp { get; set; }
}