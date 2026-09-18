namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;

public class GetAlreadyDonatedCoinsRequest
{
    public GetAlreadyDonatedCoinsRequest(long aid)
    {
        this.aid = aid;
    }

    public string jsonp { get; set; } = "jsonp";

    public long aid { get; set; }

    //public string callback { get; set; } = $"jsonCallback_bili_{new Random().Next(10000, 99999)}{new Random().Next(10000, 99999)}";
}
