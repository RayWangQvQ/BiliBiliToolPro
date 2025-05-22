using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;

public class SearchUpArticlesResponse
{
    public List<ArticleInfo> Articles { get; set; } = [];
    public int Count { get; set; }
}

public class ArticleInfo
{
    public long Id { get; set; }

    public required string Title { get; set; }
}
