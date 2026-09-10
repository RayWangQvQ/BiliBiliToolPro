using UpInfoDto = Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo.UpInfo;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;

public class GetFollowingsResponse
{
    public List<UpInfoDto> List { get; set; } = [];

    public int Total { get; set; }
}
