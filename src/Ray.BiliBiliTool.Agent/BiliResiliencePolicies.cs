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
    /// Never replay writes automatically: an error can arrive after the server applied them.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> MutatingPolicy() =>
        Policy.NoOpAsync<HttpResponseMessage>();

    public static IAsyncPolicy<HttpResponseMessage> ForRequest(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // These legacy GET routes perform actions rather than read state.
        if (
            request.RequestUri?.AbsolutePath
            is "/xlive/web-ucenter/v1/sign/DoSign"
                or "/pay/v1/Exchange/silver2coin"
                or "/xlive/rdata-interface/v1/heartbeat/webHeartBeat"
        )
        {
            return MutatingPolicy();
        }
        return
            request.Method == HttpMethod.Get
            || request.Method == HttpMethod.Head
            || request.Method == HttpMethod.Options
            ? ReadOnlyPolicy()
            : MutatingPolicy();
    }
}
