using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;
using Refit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class QingLongPersistenceTests
{
    [Theory]
    [InlineData("success", false, true)]
    [InlineData("success", true, true)]
    [InlineData("write-rejected", false, false)]
    [InlineData("write-rejected", true, false)]
    [InlineData("write-missing-data", false, false)]
    [InlineData("write-missing-data", true, false)]
    [InlineData("write-empty-data", false, false)]
    [InlineData("write-empty-data", true, false)]
    [InlineData("write-wrong-value", false, false)]
    [InlineData("write-wrong-value", true, false)]
    [InlineData("write-wrong-name", false, false)]
    [InlineData("write-wrong-name", true, false)]
    [InlineData("write-wrong-id", true, false)]
    [InlineData("auth-rejected", false, false)]
    [InlineData("auth-missing-data", false, false)]
    [InlineData("auth-empty-token", false, false)]
    [InlineData("auth-empty-type", false, false)]
    [InlineData("query-rejected", false, false)]
    [InlineData("query-missing-data", false, false)]
    [InlineData("write-connection-failure", false, false)]
    public async Task SaveCookie_RequiresAuthenticationAndConfirmedPersistence(
        string scenario,
        bool update,
        bool expected
    )
    {
        var cookie = new BiliCookie(
            new()
            {
                ["DedeUserID"] = "12",
                ["SESSDATA"] = "synthetic-session",
                ["bili_jct"] = "synthetic-csrf",
            }
        );
        var handler = new ScenarioHandler(scenario, update, cookie.CookieStr);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://qinglong.invalid"),
        };
        var api = RestService.For<IQingLongApi>(client);
        var logger = new PersistenceLogger();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Ray_PlatformType"] = "QingLong" }
            )
            .Build();
        var service = new LoginDomainService(
            logger,
            null!,
            null!,
            api,
            null!,
            null!,
            null!,
            configuration,
            Options.Create(
                new QingLongOptions
                {
                    ClientId = "synthetic-client",
                    ClientSecret = "synthetic-secret",
                }
            ),
            Options.Create(new BaihuOptions()),
            Options.Create(new DaiDaiOptions())
        );

        var result = await service.SaveCookieToQinLongAsync(cookie, CancellationToken.None);

        Assert.Equal(expected, result);
        if (scenario.StartsWith("auth-"))
            Assert.Equal(1, handler.Requests);
        else if (scenario.StartsWith("query-"))
            Assert.Equal(2, handler.Requests);
        else
            Assert.Equal(3, handler.Requests);
        Assert.Equal(
            scenario.StartsWith("auth-") || scenario.StartsWith("query-") ? 0 : 1,
            handler.Writes
        );
        if (!expected)
        {
            Assert.DoesNotContain(
                logger.Messages,
                message => message.Contains("新增成功") || message.Contains("更新成功")
            );
            Assert.DoesNotContain(logger.Messages, message => message.Contains("版本高于2.18"));
            Assert.Contains(logger.Messages, message => message.Contains("OpenAPI"));
        }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(500)]
    public async Task BusinessErrorWithoutData_IsAvailableToTheCaller(int code)
    {
        using var client = new HttpClient(new ErrorHandler(code))
        {
            BaseAddress = new Uri("https://qinglong.invalid"),
        };
        var response = await RestService
            .For<IQingLongApi>(client)
            .GetTokenAsync("synthetic", "synthetic");
        Assert.Equal(code, response.Code);
        Assert.Null(response.Data);
    }

    [Fact]
    public void SuccessfulEnvironmentStillRequiresItsTimestamp()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<QingLongEnv>(
                "{\"id\":1,\"name\":\"synthetic\",\"value\":\"synthetic\"}"
            )
        );
    }

    private sealed class ScenarioHandler(string scenario, bool update, string cookie)
        : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public int Writes { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            object payload;
            if (request.RequestUri!.AbsolutePath == "/open/auth/token")
            {
                payload =
                    scenario == "auth-missing-data"
                        ? new { code = 401 }
                        : (object)
                            new
                            {
                                code = scenario == "auth-rejected" ? 401 : 200,
                                data = new
                                {
                                    token = scenario == "auth-empty-token" ? "" : "synthetic-token",
                                    token_type = scenario == "auth-empty-type" ? "" : "Bearer",
                                },
                            };
            }
            else if (request.Method == HttpMethod.Get)
            {
                payload =
                    scenario == "query-missing-data"
                        ? new { code = 200 }
                        : (object)
                            new
                            {
                                code = scenario == "query-rejected" ? 403 : 200,
                                data = update
                                    ? new[]
                                    {
                                        EnvironmentRow(
                                            1,
                                            "Ray_BiliBiliCookies__0",
                                            "DedeUserID=12; SESSDATA=synthetic-old"
                                        ),
                                    }
                                    : [],
                            };
            }
            else
            {
                Writes++;
                if (scenario == "write-connection-failure")
                    throw new HttpRequestException("synthetic connection failure");
                if (scenario == "write-missing-data")
                    payload = new { code = 200 };
                else
                {
                    var acknowledgement = EnvironmentRow(
                        scenario == "write-wrong-id" ? 2 : 1,
                        scenario == "write-wrong-name" ? "unrelated" : "Ray_BiliBiliCookies__0",
                        scenario == "write-wrong-value" ? "stale" : cookie
                    );
                    payload = new
                    {
                        code = scenario == "write-rejected" ? 400 : 200,
                        data = update
                            ? scenario == "write-empty-data"
                                ? null
                                : (object)acknowledgement
                            : scenario == "write-empty-data"
                                ? Array.Empty<object>()
                                : new[] { acknowledgement },
                    };
                }
            }
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
            );
        }

        private static object EnvironmentRow(long id, string name, string value) =>
            new
            {
                id,
                name,
                value,
                timestamp = "synthetic-time",
            };
    }

    private sealed class ErrorHandler(int code) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { code, message = "synthetic error" }),
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
            );
    }

    private sealed class PersistenceLogger : ILogger<LoginDomainService>
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogLevel level) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(
            LogLevel level,
            EventId id,
            TState state,
            Exception? error,
            Func<TState, Exception?, string> formatter
        ) => Messages.Add(formatter(state, error));
    }
}
