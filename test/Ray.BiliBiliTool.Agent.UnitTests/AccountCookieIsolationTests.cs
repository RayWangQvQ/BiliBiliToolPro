using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.DaiDai;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class AccountCookieIsolationTests
{
    private const string OldCookie = "SESSDATA=synthetic-previous-account";
    private const string NewCookie = "SESSDATA=synthetic-current-account";

    public static IEnumerable<object[]> BiliClients()
    {
        foreach (
            var type in new[]
            {
                typeof(INavApi),
                typeof(IApiApi),
                typeof(IShowApi),
                typeof(IPassportApi),
                typeof(ILiveTraceApi),
                typeof(IHomeApi),
                typeof(IMangaApi),
                typeof(IAccountApi),
                typeof(ILiveApi),
            }
        )
        {
            yield return [type];
        }
    }

    [Theory]
    [MemberData(nameof(BiliClients))]
    public void BiliClient_DisablesPooledAutomaticCookies(Type type)
    {
        using var services = BuildServices();
        Assert.False(UsesCookies(PrimaryHandler(services, type)));
    }

    [Theory]
    [MemberData(nameof(BiliClients))]
    public async Task BiliClient_AccountChangeDoesNotAppendPreviousAccount(Type type)
    {
        using var services = BuildServices();
        var handler = Handler(services, type);
        DisableProxy(handler);
        await using var server = new CookieServer();
        using var first = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = server.Address,
        };
        using var second = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = server.Address,
        };
        using var seed = await first.GetAsync("seed");
        using var request = new HttpRequestMessage(HttpMethod.Get, "echo");
        request.Headers.Add("Cookie", NewCookie);
        using var response = await second.SendAsync(request);
        Assert.Equal(NewCookie, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(BiliClients))]
    public async Task BiliClient_AnonymousRequestRemainsAnonymous(Type type)
    {
        using var services = BuildServices();
        var handler = Handler(services, type);
        DisableProxy(handler);
        await using var server = new CookieServer();
        using var first = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = server.Address,
        };
        using var second = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = server.Address,
        };
        using var seed = await first.GetAsync("seed");
        using var response = await second.GetAsync("echo");
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(BiliClients))]
    public async Task BiliClient_SetCookieHeadersRemainAvailableForExplicitLoginMerge(Type type)
    {
        using var services = BuildServices();
        var handler = Handler(services, type);
        DisableProxy(handler);
        await using var server = new CookieServer();
        using var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = server.Address,
        };
        using var response = await client.GetAsync("seed");
        Assert.Equal([OldCookie + "; Path=/"], response.Headers.GetValues("Set-Cookie"));
    }

    [Theory]
    [InlineData(typeof(IQingLongApi))]
    [InlineData(typeof(IBaihuApi))]
    [InlineData(typeof(IDaiDaiApi))]
    public void PanelClients_KeepExistingCookieHandlerBehavior(Type type)
    {
        using var services = BuildServices();
        Assert.True(UsesCookies(PrimaryHandler(services, type)));
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<SecurityOptions>(options =>
            options.IntervalSecondsBetweenRequestApi = 0
        );
        services.AddBiliBiliClientApi(configuration);
        return services.BuildServiceProvider();
    }

    private static HttpMessageHandler Handler(ServiceProvider services, Type type)
    {
        var name = services
            .GetServices<IConfigureOptions<HttpClientFactoryOptions>>()
            .OfType<ConfigureNamedOptions<HttpClientFactoryOptions>>()
            .Select(options => options.Name)
            .Where(name => name?.Contains(type.Name, StringComparison.Ordinal) == true)
            .Distinct()
            .Single()!;
        return services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
    }

    private static HttpMessageHandler PrimaryHandler(ServiceProvider services, Type type) =>
        PrimaryHandler(Handler(services, type));

    private static HttpMessageHandler PrimaryHandler(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler delegated)
        {
            handler = delegated.InnerHandler!;
        }
        return handler;
    }

    private static bool UsesCookies(HttpMessageHandler handler) =>
        handler switch
        {
            HttpClientHandler client => client.UseCookies,
            SocketsHttpHandler sockets => sockets.UseCookies,
            _ => throw new InvalidOperationException("Unexpected primary HTTP transport"),
        };

    private static void DisableProxy(HttpMessageHandler handler)
    {
        switch (PrimaryHandler(handler))
        {
            case HttpClientHandler client:
                client.UseProxy = false;
                break;
            case SocketsHttpHandler sockets:
                sockets.UseProxy = false;
                break;
            default:
                throw new InvalidOperationException("Unexpected primary HTTP transport");
        }
    }

    // A real loopback transport exercises the factory's cookie handling without external traffic.
    private sealed class CookieServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Task _worker;
        public Uri Address { get; }

        public CookieServer()
        {
            var portListener = new TcpListener(IPAddress.Loopback, 0);
            portListener.Start();
            var port = ((IPEndPoint)portListener.LocalEndpoint).Port;
            portListener.Stop();
            Address = new Uri($"http://127.0.0.1:{port}/");
            _listener.Prefixes.Add(Address.ToString());
            _listener.Start();
            _worker = ServeAsync();
        }

        private async Task ServeAsync()
        {
            try
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    if (context.Request.Url!.AbsolutePath == "/seed")
                    {
                        context.Response.Headers.Add("Set-Cookie", OldCookie + "; Path=/");
                    }
                    var bytes = Encoding.UTF8.GetBytes(context.Request.Headers["Cookie"] ?? "");
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
            }
            catch (HttpListenerException) when (!_listener.IsListening) { }
            catch (ObjectDisposedException) when (!_listener.IsListening) { }
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Close();
            await _worker.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
