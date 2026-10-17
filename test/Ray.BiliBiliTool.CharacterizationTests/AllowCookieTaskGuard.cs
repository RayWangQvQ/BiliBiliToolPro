using Ray.BiliBiliTool.Application.Contracts.Cookies;

namespace Ray.BiliBiliTool.CharacterizationTests;

internal sealed class AllowCookieTaskGuard : ICookieTaskGuard
{
    public Task EnsureValidAsync(
        string userId,
        string cookie,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;
}
