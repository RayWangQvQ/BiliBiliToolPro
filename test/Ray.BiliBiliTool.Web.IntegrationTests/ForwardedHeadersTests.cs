using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Web.Extensions;
using Xunit;

namespace Ray.BiliBiliTool.Web.IntegrationTests;

public class ForwardedHeadersTests
{
    [Theory]
    [InlineData("203.0.113.10", "203.0.113.10", "https")]
    [InlineData("203.0.113.10", "203.0.113.11", "http")]
    [InlineData("127.0.0.1", null, "https")]
    public async Task OnlyTrustedProxy_RestoresHttpsBeforeLoginRedirect(
        string source,
        string? trusted,
        string expected
    )
    {
        var values = new Dictionary<string, string?>();
        if (trusted is not null)
            values["ReverseProxy:KnownProxies:0"] = trusted;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        using var services = new ServiceCollection()
            .AddBiliForwardedHeaders(configuration)
            .BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("panel.example.com");
        context.Connection.RemoteIpAddress = IPAddress.Parse(source);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        var middleware = new ForwardedHeadersMiddleware(
            ctx =>
            {
                ctx.Response.Headers.Location = $"{ctx.Request.Scheme}://{ctx.Request.Host}/login";
                return Task.CompletedTask;
            },
            NullLoggerFactory.Instance,
            options
        );
        await middleware.Invoke(context);
        Assert.Equal($"{expected}://panel.example.com/login", context.Response.Headers.Location);
    }

    [Theory]
    [InlineData("ReverseProxy:KnownProxies:0", "invalid")]
    [InlineData("ReverseProxy:KnownNetworks:0", "invalid")]
    public void InvalidTrustConfiguration_IsRejected(string key, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value })
            .Build();
        using var services = new ServiceCollection()
            .AddBiliForwardedHeaders(configuration)
            .BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() =>
            services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value
        );
    }
}
