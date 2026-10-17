namespace Ray.BiliBiliTool.Application.Contracts.Cookies;

public interface ICookieTaskGuard
{
    Task EnsureValidAsync(
        string userId,
        string cookie,
        CancellationToken cancellationToken = default
    );

    Task CheckNowAsync(
        string userId,
        string cookie,
        CancellationToken cancellationToken = default
    ) => EnsureValidAsync(userId, cookie, cancellationToken);
}
