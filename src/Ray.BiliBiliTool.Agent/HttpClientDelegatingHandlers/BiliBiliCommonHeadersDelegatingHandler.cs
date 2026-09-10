namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;

/// <summary>
/// Injects common Bilibili request headers with AddIfNotExist semantics.
/// Replaces the [AppendHeader] attributes that were on IBiliBiliApi in WebApiClientCore.
/// </summary>
public class BiliBiliCommonHeadersDelegatingHandler : DelegatingHandler
{
    private static readonly IReadOnlyDictionary<string, string> _commonHeaders = new Dictionary<
        string,
        string
    >
    {
        ["Accept"] = "application/json, text/plain, */*",
        ["Accept-Language"] = "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6",
        ["Sec-Fetch-Dest"] = "empty",
        ["Sec-Fetch-Mode"] = "cors",
        ["Sec-Fetch-Site"] = "same-site",
        ["Connection"] = "keep-alive",
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        foreach (var (name, value) in _commonHeaders)
        {
            if (!request.Headers.Contains(name))
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
