namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

public class MedalWallResponse
{
    public List<MedalWallDto> List { get; set; } = [];
}

public class MedalWallDto
{
    public int Live_status { get; set; }

    public required string Target_name { get; set; }

    public required string Link { get; set; }

    public required MedalInfoDto Medal_info { get; set; }
}

public class MedalInfoDto
{
    public required string Medal_name { get; set; }

    public long Medal_id { get; set; }

    public long Target_id { get; set; }

    public int Level { get; set; }
}
