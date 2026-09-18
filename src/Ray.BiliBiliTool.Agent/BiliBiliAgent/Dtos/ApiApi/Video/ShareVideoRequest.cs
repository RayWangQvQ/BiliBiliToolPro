namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;

public class ShareVideoRequest
{
    public ShareVideoRequest(long aid, string csrf)
    {
        this.aid = aid;
        this.csrf = csrf;
    }

    public long aid { get; set; }

    public string csrf { get; set; }

    public string eab_x { get; set; } = "1";

    public string ramval { get; set; } = $"{new Random().Next(3, 20)}";

    public string source { get; set; } = "web_normal";

    public string ga { get; set; } = "1";
}
