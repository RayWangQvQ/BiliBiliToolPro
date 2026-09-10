namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

public class SignRequest
{
    public string? csrf { get; set; }

    public string statistics { get; set; } =
        "{\"appId\":1,\"platform\":3,\"version\":\"6.85.0\",\"abtest\":\"\"}";
}
