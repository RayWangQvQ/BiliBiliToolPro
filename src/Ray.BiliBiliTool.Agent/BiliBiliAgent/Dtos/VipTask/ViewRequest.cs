namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class ViewRequest : BaseAppRequest
{
    public ViewRequest(string position)
    {
        this.position = position;
    }

    public string position { get; }

    public string c_locale { get; } = "zh_CN";

    public string channel { get; } = Constants.Channel;

    public string s_locale { get; } = "zh_CN";
}
