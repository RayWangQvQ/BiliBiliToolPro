using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;

public class LogDelegatingHandler(ILogger<LogDelegatingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        //记录请求内容
        logger.LogDebug(
            "发起请求：[{method}] {uri}",
            request.Method,
            HttpDiagnosticRedactor.RedactUri(request.RequestUri)
        );

        if (request.Content != null)
        {
            var requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug(
                "请求Content： {content}",
                HttpDiagnosticRedactor.RedactBody(
                    requestContent,
                    request.Content.Headers.ContentType?.MediaType
                )
            );
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogDebug(
            "返回Content：{content}",
            HttpDiagnosticRedactor.RedactBody(
                content,
                response.Content.Headers.ContentType?.MediaType
            )
        );

        return response;
    }
}
