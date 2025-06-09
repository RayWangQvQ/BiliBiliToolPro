namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Passport;

public class TokenDto
{
    public required string Url { get; set; }
    public required string Refresh_token { get; set; }
    public long Timestamp { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
}
