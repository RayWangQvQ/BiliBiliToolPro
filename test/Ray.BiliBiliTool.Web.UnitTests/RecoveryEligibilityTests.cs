using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class RecoveryEligibilityTests : IDisposable
{
    [Theory]
    [InlineData(5, 1, 1, false, false)]
    [InlineData(5, 1, 5, true, false)]
    [InlineData(2, 1, 2, true, false)]
    [InlineData(2, 2, 2, true, true)]
    [InlineData(0, 0, 0, false, true)]
    public async Task CoinRecoveryUsesConfiguredGoalBeforeAndAfterExecution(
        int target,
        int initial,
        int after,
        bool complete,
        bool skipped
    )
    {
        _config["DailyTaskConfig:NumberOfCoins"] = target.ToString();
        var account = DispatchProxy.Create<IAccountDomainService, Proxy>();
        ((Proxy)account).Call = _ => Task.FromResult(new DailyTaskInfo());
        var coin = DispatchProxy.Create<ICoinDomainService, Proxy>();
        var donated = initial;
        ((Proxy)coin).Call = _ => Task.FromResult(donated);
        var donate = DispatchProxy.Create<IDonateCoinDomainService, Proxy>();
        var submissions = 0;
        ((Proxy)donate).Call = _ =>
        {
            submissions++;
            donated = after;
            return Task.CompletedTask;
        };
        using var services = Apps();
        var result = await Build(services, account, coins: coin, donate: donate)
            .RedoAsync(91001, "DailyTaskAppService", "DonateCoin");
        Assert.Equal(complete, result.Success);
        Assert.Equal(skipped, result.Skipped);
        Assert.Equal(skipped ? 0 : 1, submissions);
        using var db = _factory.CreateDbContext();
        if (skipped)
            Assert.Empty(db.TaskRecords);
        else
            Assert.Equal(
                complete ? TaskRecordStatus.Success : TaskRecordStatus.Pending,
                Assert.Single(db.TaskRecords).Status
            );
    }

    [Theory]
    [InlineData(TaskRecordTrigger.Manual, true)]
    [InlineData(TaskRecordTrigger.Auto, false)]
    public async Task RecoveryPassesExplicitTriggerToWatchPolicy(
        TaskRecordTrigger trigger,
        bool manual
    )
    {
        bool? observed = null;
        using var services = Apps(
            new App(
                "MangaTaskAppService",
                (_, _) =>
                {
                    observed = LiveFansMedalWatchScope.IsManual;
                    return Task.CompletedTask;
                }
            )
        );
        var result = await Build(services).RedoAsync(91001, "MangaTaskAppService", null, trigger);
        Assert.True(result.Success);
        Assert.Equal(manual, observed);
        Assert.False(LiveFansMedalWatchScope.IsManual);
    }

    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"recovery-{Guid.NewGuid():N}.db"
    );
    private readonly ServiceProvider _dbServices;
    private readonly IDbContextFactory<BiliDbContext> _factory;
    private readonly TaskRecordWriter _writer;
    private readonly IConfigurationRoot _config;

    public RecoveryEligibilityTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] = $"Data Source={_path}",
                    ["BiliBiliCookies:0"] =
                        "DedeUserID=91001;bili_jct=synthetic;SESSDATA=synthetic",
                    ["BiliBiliCookies:1"] =
                        "DedeUserID=91002;bili_jct=synthetic;SESSDATA=synthetic",
                }
            )
            .Build();
        _dbServices = new ServiceCollection()
            .AddSingleton<IConfiguration>(_config)
            .AddDbContextFactory<BiliDbContext>()
            .BuildServiceProvider();
        _factory = _dbServices.GetRequiredService<IDbContextFactory<BiliDbContext>>();
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
        _writer = new(_factory, NullLogger<TaskRecordWriter>.Instance);
    }

    [Fact]
    public async Task LongRecoveryDoesNotBlockOtherAccountsOrTaskGroups()
    {
        var entered = Signal();
        var release = Signal();
        using var services = Apps(
            new App(
                "MangaTaskAppService",
                async (_, _) =>
                {
                    entered.TrySetResult();
                    await release.Task;
                }
            ),
            new App("ChargeTaskAppService", (_, _) => Task.CompletedTask)
        );
        var today = Build(services);
        var first = today.RedoAsync(91001, "MangaTaskAppService", null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            Assert.True(
                (
                    await today
                        .RedoAsync(91002, "ChargeTaskAppService", null)
                        .WaitAsync(TimeSpan.FromSeconds(5))
                ).Success
            );
            Assert.True(
                (
                    await today
                        .RedoAsync(91001, "ChargeTaskAppService", null)
                        .WaitAsync(TimeSpan.FromSeconds(5))
                ).Success
            );
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await first;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedRecoveryRechecksCompletionAndSwitches(bool disable)
    {
        var entered = Signal();
        var release = Signal();
        var calls = 0;
        using var services = Apps(
            new App(
                "MangaTaskAppService",
                async (_, _) =>
                {
                    Interlocked.Increment(ref calls);
                    entered.TrySetResult();
                    await release.Task;
                }
            )
        );
        var today = Build(services);
        var first = today.RedoAsync(91001, "MangaTaskAppService", null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = today.RedoAsync(91001, "MangaTaskAppService", null);
        Assert.False(second.IsCompleted);
        if (disable)
            _config["MangaTaskConfig:IsEnable"] = "false";
        release.TrySetResult();
        Assert.True((await first).Success);
        var result = await second.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result.Skipped);
        Assert.Equal(1, calls);
        using var db = _factory.CreateDbContext();
        Assert.Single(db.TaskRecords);
    }

    [Fact]
    public async Task AutoAttemptLimitIsCheckedAfterWaiting()
    {
        var entered = Signal();
        var release = Signal();
        var calls = 0;
        using var services = Apps(
            new App(
                "MangaTaskAppService",
                async (_, _) =>
                {
                    calls++;
                    entered.TrySetResult();
                    await release.Task;
                    throw new InvalidOperationException("synthetic refusal");
                }
            )
        );
        for (var i = 0; i < 2; i++)
            await _writer.WriteAsync(
                91001,
                "MangaTaskAppService",
                null,
                TaskRecordStatus.Failed,
                "synthetic",
                TaskRecordTrigger.Auto
            );
        var today = Build(services);
        var first = today.RedoAsync(91001, "MangaTaskAppService", null, TaskRecordTrigger.Auto);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = today.RedoAsync(91001, "MangaTaskAppService", null, TaskRecordTrigger.Auto);
        release.TrySetResult();
        await first;
        var result = await second.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result.Skipped);
        Assert.Contains("3 次", result.Message);
        Assert.Equal(1, calls);
        using var db = _factory.CreateDbContext();
        Assert.Equal(3, db.TaskRecords.Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FreshPlatformCompletionOrQueryFailureDoesNotExecute(bool complete)
    {
        var account = DispatchProxy.Create<IAccountDomainService, Proxy>();
        ((Proxy)account).Call = _ =>
            complete
                ? Task.FromResult(new DailyTaskInfo { Watch = true })
                : Task.FromException<DailyTaskInfo>(
                    new HttpRequestException("synthetic transport failure")
                );
        using var services = Apps();
        var result = await Build(services, account)
            .RedoAsync(91001, "DailyTaskAppService", "Watch");
        Assert.True(result.Skipped);
        Assert.Equal(complete, result.Success);
        using var db = _factory.CreateDbContext();
        Assert.Empty(db.TaskRecords);
    }

    [Fact]
    public async Task CancellationDoesNotWriteFailureOrConsumeAutoAttempt()
    {
        var entered = Signal();
        using var services = Apps(
            new App(
                "MangaTaskAppService",
                async (_, token) =>
                {
                    entered.TrySetResult();
                    await Task.Delay(Timeout.Infinite, token);
                }
            )
        );
        using var cancel = new CancellationTokenSource();
        var task = Build(services)
            .RedoAsync(91001, "MangaTaskAppService", null, TaskRecordTrigger.Auto, cancel.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        using var db = _factory.CreateDbContext();
        Assert.Empty(db.TaskRecords);
    }

    [Fact]
    public async Task ConcurrentRecoveryUsesSeparateServiceInstancesAndDisposesTheirScopes()
    {
        var account = DispatchProxy.Create<IAccountDomainService, Proxy>();
        ((Proxy)account).Call = _ => Task.FromResult(new DailyTaskInfo());
        var used = new System.Collections.Concurrent.ConcurrentBag<int>();
        var disposed = 0;
        var instances = 0;
        using var services = new ServiceCollection()
            .AddScoped<IVideoDomainService>(_ =>
            {
                var id = Interlocked.Increment(ref instances);
                var video = DispatchProxy.Create<IVideoDomainService, Proxy>();
                ((Proxy)video).OnDispose = () => Interlocked.Increment(ref disposed);
                ((Proxy)video).Call = method =>
                    method.Name switch
                    {
                        "GetRandomVideoForWatchAndShare" => Task.FromResult(
                            new Ray.BiliBiliTool.DomainService.Dtos.VideoInfoDto
                            {
                                Aid = "1",
                                Bvid = "synthetic",
                                Title = "synthetic",
                            }
                        ),
                        "WatchVideo" => Watch(),
                        _ => throw new NotSupportedException(method.Name),
                    };
                Task Watch()
                {
                    used.Add(id);
                    return Task.CompletedTask;
                }
                return video;
            })
            .AddScoped<TaskRecoveryExecutor>(provider =>
                new(
                    new CookieStrFactory<BiliCookie>(_config),
                    _config,
                    account,
                    provider.GetRequiredService<IVideoDomainService>(),
                    null!,
                    null!,
                    provider,
                    NullLogger<TaskRecoveryExecutor>.Instance,
                    new Guard()
                )
            )
            .BuildServiceProvider();
        var today = Build(services, account, services.GetRequiredService<IServiceScopeFactory>());
        await Task.WhenAll(
            today.RedoAsync(91001, "DailyTaskAppService", "Watch"),
            today.RedoAsync(91002, "DailyTaskAppService", "Watch")
        );
        Assert.Equal(2, used.Distinct().Count());
        Assert.Equal(2, disposed);
    }

    private TodayTaskService Build(
        IServiceProvider services,
        IAccountDomainService? account = null,
        IServiceScopeFactory? scopeFactory = null,
        ICoinDomainService? coins = null,
        IDonateCoinDomainService? donate = null
    )
    {
        var cookies = new CookieStrFactory<BiliCookie>(_config);
        var coin = coins ?? DispatchProxy.Create<ICoinDomainService, Proxy>();
        if (coins is null)
            ((Proxy)coin).Call = _ => Task.FromResult(0);
        var executor = new TaskRecoveryExecutor(
            cookies,
            _config,
            account!,
            null!,
            donate!,
            null!,
            services,
            NullLogger<TaskRecoveryExecutor>.Instance,
            new Guard()
        );
        return new(
            cookies,
            _config,
            _factory,
            null!,
            account!,
            coin,
            null!,
            null!,
            executor,
            _writer,
            NullLogger<TodayTaskService>.Instance,
            null!,
            scopeFactory
        );
    }

    private static TaskCompletionSource Signal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static ServiceProvider Apps(params App[] apps)
    {
        var services = new ServiceCollection();
        foreach (var app in apps)
            services.AddSingleton<IAccountTaskAppService>(app);
        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _dbServices.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private sealed class App(string key, Func<long, CancellationToken, Task> action)
        : IAccountTaskAppService
    {
        public string TaskKey => key;

        public Task DoTaskAsync(CancellationToken token = default) =>
            throw new NotSupportedException();

        public Task DoTaskForAccountAsync(long userId, CancellationToken token = default) =>
            action(userId, token);
    }

    private sealed class Guard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken token = default
        ) => Task.CompletedTask;
    }

    public class Proxy : DispatchProxy, IDisposable
    {
        public Action? OnDispose { get; set; }

        public void Dispose() => OnDispose?.Invoke();

        public Func<MethodInfo, object?> Call { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!);
    }
}
