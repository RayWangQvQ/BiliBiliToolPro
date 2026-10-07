namespace Ray.BiliBiliTool.Application.Contracts.Cookies;

public sealed record CookieCheckState(
    string Fingerprint,
    DateOnly CheckedDate,
    bool IsValid,
    DateOnly? NotifiedDate = null,
    DateTimeOffset? NotificationAttempt = null
);

public interface ICookieCheckStateStore
{
    Task<CookieCheckState?> ReadAsync(string userId, CancellationToken cancellationToken);
    Task WriteAsync(string userId, CookieCheckState state, CancellationToken cancellationToken);
}

public interface ICookieExpiryNotifier
{
    Task<bool> SendAsync(string maskedAccount, CancellationToken cancellationToken);
}
