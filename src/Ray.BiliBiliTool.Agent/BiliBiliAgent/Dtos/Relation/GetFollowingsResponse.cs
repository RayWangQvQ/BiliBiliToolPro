using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;

public class GetFollowingsResponse
{
    public List<UpInfo> List { get; set; } = [];

    public int Total { get; set; }
}
