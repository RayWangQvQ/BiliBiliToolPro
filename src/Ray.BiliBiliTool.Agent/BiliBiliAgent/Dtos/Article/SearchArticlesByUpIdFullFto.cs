namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;

public class SearchArticlesByUpIdDto
{
    public long Mid { get; set; }

    public int Pn { get; set; } = 1;

    public int Ps { get; set; } = 30;

    public string Sort { get; set; } = "publish_time";
}

public class SearchArticlesByUpIdFullDto : SearchArticlesByUpIdDto
{
    public string w_rid { get; set; }

    public long wts { get; set; }
}
