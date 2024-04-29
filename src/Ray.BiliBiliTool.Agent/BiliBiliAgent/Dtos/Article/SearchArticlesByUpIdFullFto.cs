using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;

public class SearchArticlesByUpIdDto : IWrid
{
    public long mid { get; set; }

    public int pn { get; set; } = 1;

    public int ps { get; set; } = 12;

    public string sort { get; set; } = "publish_time";

    public long web_location { get; set; } = 1550101;

    public string platform { get; set; } = "web";

    public string w_rid { get; set; }
    public long wts { get; set; }
}
