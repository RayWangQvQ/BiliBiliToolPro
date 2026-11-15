namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;

/// <summary>
/// Claim or complete a big-point task. Inherits <see cref="BaseAppRequest"/> because the v2 endpoints
/// validate the client build and reject a request without it (6007000 "please update to the latest version").
/// </summary>
public class ReceiveOrCompleteTaskRequest : BaseAppRequest
{
    public ReceiveOrCompleteTaskRequest(string taskCode)
    {
        TaskCode = taskCode;
    }

    public string TaskCode { get; set; }
}
