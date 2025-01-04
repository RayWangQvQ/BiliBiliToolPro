namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class ViewRequest
{
    public ViewRequest(string position)
    {
        this.position=position;
    }

    public string position { get; }

    public string c_locale { get; } = "zh_CN";

    public string channel { get; } = Constants.Channel;

    public int disable_rcmd { get; } = 0;

    public string mobi_app { get; } = "android";

    public string platform { get; } = "android";

    public string s_locale { get; } = "zh_CN";

    public string statistics { get; } = "{\"appId\":1,\"platform\":3,\"version\":\"6.85.0\",\"abtest\":\"\"}";
}