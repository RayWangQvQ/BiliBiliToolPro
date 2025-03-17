namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class ReceiveOrCompleteTaskRequest
{
    public ReceiveOrCompleteTaskRequest(string taskCode)
    {
        TaskCode = taskCode;
    }

    public string TaskCode { get; set; }
}
