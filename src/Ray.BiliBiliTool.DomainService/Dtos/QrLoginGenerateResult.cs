namespace Ray.BiliBiliTool.DomainService.Dtos;

public record QrLoginGenerateResult
{
    public required string QrImageBase64 { get; init; }
    public required string QrcodeKey { get; init; }
    public required string OnlineUrl { get; init; }
}
