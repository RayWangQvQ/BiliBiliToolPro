namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class GetSpaceInfoResponse
{
    public long Mid { get; set; }

    public required string Name { get; set; }

    public SpaceLiveRoomInfoDto? Live_room { get; set; }
}

public class SpaceLiveRoomInfoDto
{
    public required string Title { get; set; }

    public long Roomid { get; set; }
}
