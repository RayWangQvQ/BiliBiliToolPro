namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Video;

public class GetBangumiBySsidResponse
{
    public int Code { get; set; } = int.MinValue;

    public string? Message { get; set; }

    public required Result Result { get; set; }
}

public class Result
{
    public List<Episode> episodes { get; set; } = [];
}

public class Episode
{
    public int aid { get; set; }

    public required string bvid { get; set; }

    public int cid { get; set; }

    public int duration { get; set; }

    public int ep_id { get; set; }

    public int id { get; set; }

    public required string long_title { get; set; }

    public required string share_copy { get; set; }

    public int status { get; set; }
}
