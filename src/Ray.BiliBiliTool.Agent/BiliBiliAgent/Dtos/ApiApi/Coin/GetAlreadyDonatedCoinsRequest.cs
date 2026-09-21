namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;

public class GetAlreadyDonatedCoinsRequest
{
    public GetAlreadyDonatedCoinsRequest(long aid)
    {
        Aid = aid;
    }

    public string Jsonp { get; set; } = "jsonp";

    public long Aid { get; set; }

    //public string Callback { get; set; } = $"jsonCallback_bili_{new Random().Next(10000, 99999)}{new Random().Next(10000, 99999)}";
}
