namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;

public class AddCoinRequest
{
    public AddCoinRequest(long aid, string csrf)
    {
        this.aid = aid;
        this.csrf = csrf;
    }

    public long aid { get; set; }

    public int multiply { get; set; } = 1;

    public int select_like { get; set; } = 1;

    public string cross_domain { get; set; } = "true";

    public string csrf { get; set; }

    public string eab_x { get; set; } = "2";

    public string ramval { get; set; } = "3";

    public string source { get; set; } = "web_normal";

    public string ga { get; set; } = "1";
}
