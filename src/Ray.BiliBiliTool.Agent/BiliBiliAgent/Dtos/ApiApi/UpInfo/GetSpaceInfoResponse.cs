namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;

public class GetSpaceInfoResponse
{
    public long Mid { get; set; }

    public required string Name { get; set; }

    public required SpaceLiveRoomInfoDto Live_room { get; set; }
}

public class SpaceLiveRoomInfoDto
{
    public required string Title { get; set; }

    public long Roomid { get; set; }
}
