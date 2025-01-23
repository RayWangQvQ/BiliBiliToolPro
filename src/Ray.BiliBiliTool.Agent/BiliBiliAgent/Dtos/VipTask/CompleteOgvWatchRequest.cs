namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class CompleteOgvWatchRequest: BaseAppRequest
{
    public CompleteOgvWatchRequest(long taskId, string token)
    {
        task_id = taskId;
        this.token = token;
    }

    public long task_id { get; set; }

    public string token { get; set; }

    public string task_sign { get; set; }

    public long timestamp { get; set; }
}