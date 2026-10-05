using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;
using Xunit;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

[Collection("Host boot")]
public class ReverseProxyLoginTests
{
    [Fact]
    public async Task TrustedHttpsProxy_LoginChallengeKeepsExternalScheme()
    {
        using var factory = new WebHostFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, LoopbackProxyFilter>()
            )
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("http://panel.example.com"),
            }
        );
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/Configurations/DailyJobConfig"
        );
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https", response.Headers.Location!.Scheme);
        Assert.Equal("/login", response.Headers.Location.AbsolutePath);
    }

    private sealed class LoopbackProxyFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(
                    (context, nextMiddleware) =>
                    {
                        context.Connection.RemoteIpAddress = IPAddress.Loopback;
                        return nextMiddleware(context);
                    }
                );
                next(app);
            };
    }
}
