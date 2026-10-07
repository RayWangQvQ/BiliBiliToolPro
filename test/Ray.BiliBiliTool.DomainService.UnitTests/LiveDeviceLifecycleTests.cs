using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class LiveDeviceLifecycleTests
{
    [Theory]
    [InlineData("watchLive", false)]
    [InlineData("watchLive", true)]
    [InlineData("like", false)]
    [InlineData("like", true)]
    [InlineData("sendDanmu", false)]
    [InlineData("sendDanmu", true)]
    public async Task DisabledBatch_DoesNotInitializeOrReadTasks(string action, bool canceled)
    {
        var env = new Environment(false);
        using var cancellation = new CancellationTokenSource();
        if (canceled)
            cancellation.Cancel();

        await env.Run(true, action, cancellation.Token);

        Assert.Empty(env.InitializationCookies);
        Assert.Equal(0, env.TaskReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledInitialization_DisposesResponseWhenItArrives(bool batch)
    {
        var env = new Environment();
        var pending = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        env.Initialize = _ => pending.Task;
        using var cancellation = new CancellationTokenSource();
        var run = env.Run(batch, "watchLive", cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.False(pending.Task.IsCompleted);
        using var response = Response("synthetic-late-device");
        var content = Assert.IsType<TrackingContent>(response.Content);
        pending.SetResult(response);

        await content.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(0, env.TaskReads);
        Assert.Empty(env.Cookie.LiveBuvid);
        Assert.Equal("synthetic-login", env.Cookie.SessData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledAnonymousFallback_DisposesBothResponses(bool batch)
    {
        var env = new Environment();
        using var first = Response();
        var firstContent = Assert.IsType<TrackingContent>(first.Content);
        var pending = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        env.Initialize = attempt => attempt == 1 ? Task.FromResult(first) : pending.Task;
        using var cancellation = new CancellationTokenSource();
        var run = env.Run(batch, "watchLive", cancellation.Token);
        Assert.Equal(2, env.InitializationCookies.Count);
        Assert.Empty(env.InitializationCookies[1]);
        Assert.True(firstContent.Disposed.Task.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        using var late = Response("synthetic-late-device");
        var lateContent = Assert.IsType<TrackingContent>(late.Content);
        pending.SetResult(late);

        await lateContent.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(0, env.TaskReads);
        Assert.Empty(env.Cookie.LiveBuvid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeCompletedResponseUse_PreventsCookieMutation(bool batch)
    {
        var env = new Environment();
        using var response = Response("synthetic-canceled-device");
        var content = Assert.IsType<TrackingContent>(response.Content);
        using var cancellation = new CancellationTokenSource();
        env.Initialize = _ =>
        {
            cancellation.Cancel();
            return Task.FromResult(response);
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            env.Run(batch, "watchLive", cancellation.Token)
        );

        Assert.True(content.Disposed.Task.IsCompleted);
        Assert.Empty(env.Cookie.LiveBuvid);
        Assert.Equal(0, env.TaskReads);
    }

    [Fact]
    public async Task SequentialAccounts_GetIndependentDevicesAndKeepCredentials()
    {
        var env = new Environment();
        env.Initialize = attempt => Task.FromResult(Response($"synthetic-device-{attempt}"));
        for (var index = 1; index <= 3; index++)
        {
            env.Cookie = Cookie(index);
            await env.Run(false, "watchLive", CancellationToken.None);
            Assert.Equal($"synthetic-device-{index}", env.Cookie.LiveBuvid);
            Assert.Equal("synthetic-login", env.Cookie.SessData);
            Assert.Equal("synthetic-csrf", env.Cookie.BiliJct);
        }
        Assert.Equal(3, env.InitializationCookies.Count);
        Assert.Equal(3, env.TaskReads);
    }

    [Fact]
    public async Task SuccessfulInitialization_DisposesResponseBeforeTaskLookup()
    {
        var env = new Environment();
        using var response = Response("synthetic-device");
        var content = Assert.IsType<TrackingContent>(response.Content);
        env.Initialize = _ => Task.FromResult(response);
        env.BeforeTaskRead = () => Assert.True(content.Disposed.Task.IsCompleted);

        await env.Run(false, "watchLive", CancellationToken.None);

        Assert.Equal(1, env.TaskReads);
        Assert.Equal("synthetic-device", env.Cookie.LiveBuvid);
    }

    private static BiliCookie Cookie(int index) =>
        new(
            new()
            {
                ["DedeUserID"] = index.ToString(),
                ["SESSDATA"] = "synthetic-login",
                ["bili_jct"] = "synthetic-csrf",
            }
        );

    private static HttpResponseMessage Response(string? device = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new TrackingContent("{\"code\":0}"),
        };
        if (device is not null)
            response.Headers.TryAddWithoutValidation("Set-Cookie", $"LIVE_BUVID={device}; Path=/");
        return response;
    }

    private sealed class TrackingContent(string text) : StringContent(text)
    {
        public TaskCompletionSource Disposed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Disposed.TrySetResult();
        }
    }

    private sealed class Environment
    {
        public BiliCookie Cookie = LiveDeviceLifecycleTests.Cookie(1);
        public List<string> InitializationCookies = [];
        public int TaskReads;
        public Func<int, Task<HttpResponseMessage>> Initialize = _ =>
            Task.FromResult(Response("synthetic-device"));
        public Action? BeforeTaskRead;
        private readonly LiveDomainService _service;

        public Environment(bool enabled = true)
        {
            var api = LiveFansMedalTaskTests.Proxy.Create<ILiveApi>(
                (method, args) =>
                {
                    if (method == "GetLiveHome")
                    {
                        InitializationCookies.Add((string)args[0]!);
                        return Initialize(InitializationCookies.Count);
                    }
                    BeforeTaskRead?.Invoke();
                    TaskReads++;
                    if (method == "GetFansMedalPanel")
                        return Task.FromResult(
                            new BiliApiResponse<FansMedalPanelResponse> { Code = 0, Data = new() }
                        );
                    Assert.Equal("GetActivatedMedalInfo", method);
                    return Task.FromResult(
                        new BiliApiResponse<ActivatedMedalResponse>
                        {
                            Code = 0,
                            Data = new() { Is_lighted = true, Reach_free_intimacy_limit = true },
                        }
                    );
                }
            );
            _service = new(
                NullLogger<LiveDomainService>.Instance,
                api,
                null!,
                null!,
                new Monitor<DailyTaskOptions>(new()),
                new Monitor<LiveLotteryTaskOptions>(new()),
                new Monitor<LiveFansMedalTaskOptions>(new() { IsEnable = enabled }),
                new Monitor<SecurityOptions>(new()),
                new Monitor<Silver2CoinTaskOptions>(new())
            );
        }

        public Task Run(bool batch, string action, CancellationToken token) =>
            !batch ? _service.RunFansMedalActionForAnchorAsync(Cookie, 2, 3, action, token)
            : action == "watchLive" ? _service.SendHeartBeatToFansMedalLive(Cookie, token)
            : action == "like" ? _service.LikeFansMedalLive(Cookie, token)
            : _service.SendDanmakuToFansMedalLive(Cookie, token);
    }

    private sealed class Monitor<T>(T options) : IOptionsMonitor<T>
    {
        public T CurrentValue => options;

        public T Get(string? name) => options;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
