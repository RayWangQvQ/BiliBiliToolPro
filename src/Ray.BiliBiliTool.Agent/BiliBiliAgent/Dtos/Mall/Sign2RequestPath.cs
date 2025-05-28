namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class Sign2RequestPath(string csrf)
{
    public string mobi_app { get; set; } = "android";

    public string csrf { get; set; } = csrf;

    public string platform { get; set; } = "android";
}
