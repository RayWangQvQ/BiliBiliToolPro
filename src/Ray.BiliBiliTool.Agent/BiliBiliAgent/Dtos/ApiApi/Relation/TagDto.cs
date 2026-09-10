namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;

public class TagDto
{
    public long Tagid { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// 关注up个数
    /// </summary>
    public int Count { get; set; }

    public string? Tip { get; set; }
}
