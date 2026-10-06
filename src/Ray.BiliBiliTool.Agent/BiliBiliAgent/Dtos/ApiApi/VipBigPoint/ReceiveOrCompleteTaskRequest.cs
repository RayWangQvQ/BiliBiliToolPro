namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

public class ReceiveOrCompleteTaskRequest
{
    public ReceiveOrCompleteTaskRequest(string taskCode)
    {
        TaskCode = taskCode;
    }

    public string TaskCode { get; set; }
}

public class VipPointV2TaskRequest(string taskCode) : ReceiveOrCompleteTaskRequest(taskCode)
{
    [Refit.AliasAs("csrf")]
    public string? Csrf { get; set; }

    [Refit.AliasAs("ts")]
    public long Ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Refit.AliasAs("platform")]
    public string Platform { get; set; } = "android";

    [Refit.AliasAs("mobi_app")]
    public string MobiApp { get; set; } = "android";

    [Refit.AliasAs("device")]
    public string Device { get; set; } = "android";
}
