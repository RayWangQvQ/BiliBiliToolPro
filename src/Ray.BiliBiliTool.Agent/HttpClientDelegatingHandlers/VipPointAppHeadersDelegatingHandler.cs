using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;

public class VipPointAppHeadersDelegatingHandler(IOptionsMonitor<SecurityOptions> options)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (
            request.RequestUri?.AbsolutePath
            is "/pgc/activity/score/task/receive/v2"
                or "/pgc/activity/score/task/complete/v2"
        )
        {
            request.Headers.Remove("User-Agent");
            string userAgent = options.CurrentValue.UserAgentApp;
            request.Headers.Add("User-Agent", userAgent);
            request.Headers.Add("app-key", "android64");
            request.Headers.Add("env", "prod");
            request.Headers.Add("native_api_from", "h5");
            var build = Regex.Match(userAgent, @"\bbuild/(\d+)", RegexOptions.IgnoreCase);
            if (
                build.Success
                && request.Content?.Headers.ContentType?.MediaType
                    == "application/x-www-form-urlencoded"
            )
            {
                string body = await request.Content.ReadAsStringAsync(cancellationToken);
                if (
                    !body.Split('&')
                        .Any(pair => pair.StartsWith("build=", StringComparison.Ordinal))
                )
                {
                    var previous = request.Content;
                    request.Content = new StringContent(
                        body + "&build=" + build.Groups[1].Value,
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded"
                    );
                    previous.Dispose();
                }
            }
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
