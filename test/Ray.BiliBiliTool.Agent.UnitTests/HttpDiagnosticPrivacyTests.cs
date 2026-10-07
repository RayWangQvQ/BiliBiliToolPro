using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Refit;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class HttpDiagnosticPrivacyTests
{
    private const string Sentinel = "synthetic-credential-do-not-log";

    [Theory]
    [InlineData("query")]
    [InlineData("form")]
    [InlineData("json")]
    [InlineData("response-json")]
    [InlineData("malformed-json")]
    [InlineData("text-response")]
    public async Task DebugLogging_HidesCredentialsWithoutChangingTheExchange(string scenario)
    {
        var input =
            scenario == "form" ? "csrf_token=" + Sentinel + "&amount=2"
            : scenario == "json"
                ? "{\"csrf_token\":\""
                    + Sentinel
                    + "\",\"nested\":{\"accessKey\":\""
                    + Sentinel
                    + "\"},\"amount\":2}"
            : "";
        var output =
            scenario == "response-json" ? "{\"code\":0,\"secret_key\":\"" + Sentinel + "\"}"
            : scenario == "malformed-json" ? "{\"code\":0,\"accessKey\":\"" + Sentinel + "\""
            : scenario == "text-response" ? "<input value=\"" + Sentinel + "\">"
            : "{\"code\":0}";
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://synthetic.invalid/test"
                + (scenario == "query" ? "?csrf_token=" + Sentinel + "&amount=2" : "")
        );
        if (input.Length > 0)
            request.Content = new StringContent(
                input,
                Encoding.UTF8,
                scenario == "form" ? "application/x-www-form-urlencoded" : "application/json"
            );
        var logs = new DiagnosticLogs();
        var capture = new CaptureHandler(
            output,
            scenario == "text-response" ? "text/html" : "application/json"
        );
        using var handler = new LogDelegatingHandler(
            new DiagnosticLogger<LogDelegatingHandler>(logs)
        )
        {
            InnerHandler = capture,
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var response = await invoker.SendAsync(request, CancellationToken.None);
        Assert.Equal(request.RequestUri!.OriginalString, capture.Uri);
        Assert.Equal(input, capture.Body);
        Assert.Equal(output, await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(logs.Messages, message => message.Contains(Sentinel));
    }

    [Theory]
    [InlineData("query-camel")]
    [InlineData("query-encoded")]
    [InlineData("body-json")]
    [InlineData("custom-header")]
    public async Task FailureRequestDescription_HidesCredentialVariants(string scenario)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://synthetic.invalid/test?"
                + (
                    scenario == "query-camel" ? "accessKey=" + Sentinel
                    : scenario == "query-encoded" ? "%63srf_token=" + Sentinel
                    : "amount=2"
                )
        );
        if (scenario == "body-json")
            request.Content = new StringContent(
                "{\"nested\":[{\"csrf_token\":\""
                    + Sentinel
                    + "\",\"secretKey\":\""
                    + Sentinel
                    + "\"}]}",
                Encoding.UTF8,
                "application/json"
            );
        if (scenario == "custom-header")
            request.Headers.Add("X-API-Key", Sentinel);
        var method = typeof(ServiceCollectionExtension).GetMethod(
            "DescribeRequestAsync",
            BindingFlags.Static | BindingFlags.NonPublic
        )!;
        var description = await (Task<string>)method.Invoke(null, [request])!;
        Assert.DoesNotContain(Sentinel, description);
        Assert.Contains("POST", description);
    }

    [Fact]
    public async Task RegisteredClient_ParsingFailureDoesNotExposeCredentialsInLogsOrMessage()
    {
        var logs = new DiagnosticLogs();
        var capture = new CaptureHandler(
            "{\"code\":0,\"data\":\"unexpected\",\"accessKey\":\""
                + Sentinel
                + "\",\"nested\":{\"csrf_token\":\""
                + Sentinel
                + "\"}}",
            "application/json"
        );
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
            builder.SetMinimumLevel(LogLevel.Debug).AddProvider(new DiagnosticProvider(logs))
        );
        services.Configure<SecurityOptions>(options =>
        {
            options.UserAgent = "synthetic-agent";
            options.IntervalSecondsBetweenRequestApi = 0;
        });
        services.AddBiliBiliClientApi(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.PrimaryHandler = capture
            )
        );
        using var provider = services.BuildServiceProvider();
        var error = await Assert.ThrowsAsync<ApiException>(() =>
            provider
                .GetRequiredService<IApiApi>()
                .GetCombineAsync(
                    new GetCombineRequest { csrf = Sentinel, buvid = Sentinel },
                    "SESSDATA=" + Sentinel
                )
        );
        Assert.DoesNotContain(Sentinel, error.Message);
        Assert.DoesNotContain(logs.Messages, message => message.Contains(Sentinel));
        Assert.Contains("HTTP 200", error.Message);
        Assert.Contains(Sentinel, error.Content!);
    }

    [Fact]
    public async Task DebugLogging_RetainsBusinessStatusAndNormalFields()
    {
        var logs = new DiagnosticLogs();
        using var handler = new LogDelegatingHandler(
            new DiagnosticLogger<LogDelegatingHandler>(logs)
        )
        {
            InnerHandler = new CaptureHandler(
                "{\"code\":-400,\"message\":\"synthetic failure\",\"count\":2}",
                "application/json"
            ),
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://synthetic.invalid/test?count=2"
        );
        using var response = await invoker.SendAsync(request, CancellationToken.None);
        Assert.Contains(
            logs.Messages,
            message => message.Contains("-400") && message.Contains("synthetic failure")
        );
        Assert.Contains(logs.Messages, message => message.Contains("count=2"));
    }

    [Fact]
    public async Task QingLongAuthentication_DoesNotLogClientSecretInFrameworkLogs()
    {
        var logs = new DiagnosticLogs();
        var capture = new CaptureHandler(
            "{\"code\":200,\"data\":{\"token\":\"" + Sentinel + "\",\"token_type\":\"Bearer\"}}",
            "application/json"
        );
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
            builder.SetMinimumLevel(LogLevel.Debug).AddProvider(new DiagnosticProvider(logs))
        );
        services.Configure<SecurityOptions>(options => options.UserAgent = "synthetic-agent");
        services.AddBiliBiliClientApi(configuration);
        services.PostConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder =>
                builder.PrimaryHandler = capture
            )
        );
        using var provider = services.BuildServiceProvider();
        var response = await provider
            .GetRequiredService<IQingLongApi>()
            .GetTokenAsync("synthetic", Sentinel);
        Assert.NotNull(response.Data);
        Assert.Equal(Sentinel, response.Data.token);
        Assert.Contains(Sentinel, capture.Uri!);
        Assert.DoesNotContain(logs.Messages, message => message.Contains(Sentinel));
    }

    private sealed class CaptureHandler(string body, string mediaType) : HttpMessageHandler
    {
        public string? Uri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Uri = request.RequestUri!.OriginalString;
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(body, Encoding.UTF8, mediaType),
            };
        }
    }

    private sealed class DiagnosticLogs
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class DiagnosticLogger<T>(DiagnosticLogs logs) : ILogger<T>
    {
        public bool IsEnabled(LogLevel level) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(
            LogLevel level,
            EventId id,
            TState state,
            Exception? error,
            Func<TState, Exception?, string> formatter
        ) => logs.Messages.Add(formatter(state, error));
    }

    private sealed class DiagnosticProvider(DiagnosticLogs logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new DiagnosticLogger<object>(logs);

        public void Dispose() { }
    }
}
