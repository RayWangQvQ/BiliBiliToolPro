using Ray.BiliBiliTool.Agent;

namespace Ray.BiliBiliTool.DomainService.Dtos;

public record QrLoginCheckResult
{
    public required QrLoginStatus Status { get; init; }
    public string? Message { get; init; }
    public BiliCookie? Cookie { get; init; }
}
