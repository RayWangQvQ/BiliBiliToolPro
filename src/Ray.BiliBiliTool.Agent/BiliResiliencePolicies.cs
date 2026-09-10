using Polly;
using Polly.Extensions.Http;

namespace Ray.BiliBiliTool.Agent;

public static class BiliResiliencePolicies
{
    public const int ReadOnlyRetryCount = 1;
    public static readonly TimeSpan ReadOnlyRetryBackoff = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// For read-only / idempotent clients: retries once on transient HTTP errors.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> ReadOnlyPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(ReadOnlyRetryCount, _ => ReadOnlyRetryBackoff);

    /// <summary>
    /// For side-effecting clients (coin donation, charge, live sign-in):
    /// retries only on network-level failures, not on 5xx responses.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> MutatingPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(ReadOnlyRetryCount, _ => ReadOnlyRetryBackoff);
}
