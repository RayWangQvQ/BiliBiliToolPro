namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class GetCombineRequest : BaseAppRequest
{
    public required string buvid { get; set; }
    public required string csrf { get; set; }

    public string brand { get; set; } = "Samsung";
    public string channel { get; set; } = "bili";
    public string containerName { get; set; } = "AbstractWebActivity";
    public string device { get; set; } = "phone";
}
