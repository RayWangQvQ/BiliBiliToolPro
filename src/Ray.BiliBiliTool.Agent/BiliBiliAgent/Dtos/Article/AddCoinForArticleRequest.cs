namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Article;

public class AddCoinForArticleRequest
{
    public AddCoinForArticleRequest(long cvid,long mid,string csrf)
    {
        Aid = cvid;
        Upid = mid;
        Csrf = csrf;
    }

    public long Aid { get; set; }

    public long Upid { get; set; }

    public int Multiply { get; set; } = 1;

    // 必须为2
    public int Avtype { get; private set; } = 2;

    public string Csrf { get; set; }
}
