using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Services;
using Xunit;

namespace Ray.BiliBiliTool.Web.IntegrationTests;

public class TaskRecordWriterTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"bilitool-test-{Guid.NewGuid():N}.db"
    );
    private readonly ServiceProvider _provider;
    private readonly IDbContextFactory<BiliDbContext> _factory;

    public TaskRecordWriterTests()
    {
        // BiliDbContext 的构造函数需要 IConfiguration，且连接串在 OnConfiguring 里从配置取，
        // 因此直接用 DI 构造工厂，而不是手搓 DbContextOptions。
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath};Cache=Shared",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContextFactory<BiliDbContext>();
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IDbContextFactory<BiliDbContext>>();

        using var db = _factory.CreateDbContext();
        // 用 EnsureCreated 而不是 Migrate()：既有的 AddBiliLogs 迁移里有不带 ifExists 的
        // DropTable("bili_logs")，全新库从零 Migrate 会报 "no such table: bili_logs"（历史遗留问题，
        // 与本次改动无关，线上库已应用过全部迁移所以不受影响）。
        // 迁移本身是否能正确升级线上库，由「真实库副本」的验证步骤单独覆盖。
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _provider.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task WriteAsync_SuccessfulTask_PersistsAccountAndDate()
    {
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);

        await writer.WriteAsync(
            1001,
            "DailyTaskAppService",
            "DonateCoin",
            TaskRecordStatus.Success,
            null,
            TaskRecordTrigger.Manual
        );

        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords.Where(r => r.UserId == 1001));
        Assert.Equal("DailyTaskAppService", record.TaskKey);
        Assert.Equal("DonateCoin", record.TaskItemKey);
        Assert.Equal(TaskRecordStatus.Success, record.Status);
        Assert.Equal(TaskRecordTrigger.Manual, record.Trigger);
        Assert.Equal(DateTimeOffset.Now.ToString("yyyy-MM-dd"), record.RecordDate);
    }

    [Fact]
    public async Task WriteAsync_MessageOver512Characters_TruncatesTo512Characters()
    {
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);
        var longMessage = new string('错', 600);

        await writer.WriteAsync(
            1002,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Failed,
            longMessage,
            TaskRecordTrigger.Scheduled
        );

        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords.Where(r => r.UserId == 1002));
        Assert.Equal(512, record.Message!.Length);
    }

    [Theory]
    [InlineData(511)]
    [InlineData(512)]
    public async Task WriteAsync_MessageAtOrBelowLimit_PreservesAllCharacters(int length)
    {
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);
        var message = new string('a', length);

        await writer.WriteAsync(
            1003,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Failed,
            message,
            TaskRecordTrigger.Manual
        );

        await using var db = await _factory.CreateDbContextAsync();
        Assert.Equal(message, Assert.Single(db.TaskRecords).Message);
    }

    [Fact]
    public async Task FailedRecordsEnterSummary_SuccessAndCookieExpiryDoNot()
    {
        var monitor = new CaptureMonitor();
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance, monitor);
        await writer.WriteAsync(
            1001,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Success,
            null,
            TaskRecordTrigger.Scheduled
        );
        await writer.WriteAsync(
            1001,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Failed,
            "Cookie 已过期，本次活动已跳过，请在账号管理中重新登录",
            TaskRecordTrigger.Scheduled
        );
        await writer.WriteAsync(
            1001,
            "MangaTaskAppService",
            null,
            TaskRecordStatus.Failed,
            "synthetic error containing private details",
            TaskRecordTrigger.Scheduled
        );
        Assert.Equal("MangaTaskAppService", Assert.Single(monitor.TaskKeys));
    }

    [Theory]
    [InlineData(TaskRecordTrigger.Manual)]
    [InlineData(TaskRecordTrigger.Auto)]
    public async Task RecoveryFailures_KeepExecutionRecordsWithoutQueuingReminders(
        TaskRecordTrigger trigger
    )
    {
        var monitor = new CaptureMonitor();
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance, monitor);
        await writer.WriteAsync(
            1001,
            "MangaTaskAppService",
            null,
            TaskRecordStatus.Failed,
            "synthetic recovery failure",
            trigger
        );
        Assert.Empty(monitor.TaskKeys);
        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords);
        Assert.Equal(trigger, record.Trigger);
        Assert.Equal(TaskRecordStatus.Failed, record.Status);
        Assert.Equal("synthetic recovery failure", record.Message);
    }

    [Fact]
    public async Task ManualExecutionScope_AlsoExcludesNestedScheduledRecords()
    {
        var monitor = new CaptureMonitor();
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance, monitor);
        using (new TaskFailureNotificationScope(suppress: true))
            await writer.WriteAsync(
                1001,
                "MangaTaskAppService",
                null,
                TaskRecordStatus.Failed,
                "synthetic failure",
                TaskRecordTrigger.Scheduled
            );
        Assert.Empty(monitor.TaskKeys);
        await writer.WriteAsync(
            1001,
            "DailyTaskAppService",
            null,
            TaskRecordStatus.Failed,
            "synthetic failure",
            TaskRecordTrigger.Scheduled
        );
        Assert.Equal("DailyTaskAppService", Assert.Single(monitor.TaskKeys));
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Equal(2, await db.TaskRecords.CountAsync());
    }

    [Theory]
    [InlineData(TaskRecordTrigger.Manual, "MangaTaskAppService")]
    [InlineData(TaskRecordTrigger.Manual, "VipPrivilegeTaskAppService")]
    [InlineData(TaskRecordTrigger.Auto, "MangaTaskAppService")]
    [InlineData(TaskRecordTrigger.Auto, "VipPrivilegeTaskAppService")]
    public async Task RedoAsync_SuppressesNestedRemindersAndPreservesFailureResult(
        TaskRecordTrigger trigger,
        string taskKey
    )
    {
        var monitor = new CaptureMonitor();
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance, monitor);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] =
                        "DedeUserID=1001; bili_jct=synthetic; SESSDATA=synthetic",
                }
            )
            .Build();
        using var services = new ServiceCollection()
            .AddSingleton<IAccountTaskAppService>(new FailingRecoveryTask(taskKey))
            .BuildServiceProvider();
        var cookies = new CookieStrFactory<BiliCookie>(config);
        var executor = new TaskRecoveryExecutor(
            cookies,
            config,
            null!,
            null!,
            null!,
            null!,
            services,
            NullLogger<TaskRecoveryExecutor>.Instance,
            new AllowGuard()
        );
        var today = new TodayTaskService(
            cookies,
            config,
            _factory,
            null!,
            null!,
            null!,
            null!,
            null!,
            executor,
            writer,
            NullLogger<TodayTaskService>.Instance,
            null!
        );
        var result = await today.RedoAsync(1001, taskKey, null, trigger);
        Assert.False(result.Success);
        Assert.Empty(monitor.TaskKeys);
        Assert.False(TaskFailureNotificationScope.IsSuppressed);
        await using var db = await _factory.CreateDbContextAsync();
        var record = Assert.Single(db.TaskRecords);
        Assert.Equal(taskKey, record.TaskKey);
        Assert.Null(record.TaskItemKey);
        Assert.Equal(trigger, record.Trigger);
        Assert.Equal(TaskRecordStatus.Failed, record.Status);
        Assert.Contains("synthetic recovery failure", result.Message);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task MedalRedoFeedbackReflectsPlatformGoalInsteadOfSuccessfulExecution(
        bool lit,
        bool expectedSuccess
    )
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
                }
            )
            .Build();
        using var services = new ServiceCollection()
            .AddSingleton<IAccountTaskAppService>(new SuccessfulMedalTask())
            .BuildServiceProvider();
        var cookies = new CookieStrFactory<BiliCookie>(config);
        var executor = new TaskRecoveryExecutor(
            cookies,
            config,
            null!,
            null!,
            null!,
            null!,
            services,
            NullLogger<TaskRecoveryExecutor>.Instance,
            new AllowGuard()
        );
        var medals = new ReadOnlyMedalResult(lit);
        var writer = new TaskRecordWriter(_factory, NullLogger<TaskRecordWriter>.Instance);
        var today = new TodayTaskService(
            cookies,
            config,
            _factory,
            null!,
            null!,
            null!,
            null!,
            null!,
            executor,
            writer,
            NullLogger<TodayTaskService>.Instance,
            medals
        );
        var result = await today.RedoAsync(1001, "LiveFansMedalAppService", null);
        Assert.Equal(expectedSuccess, result.Success);
        Assert.Contains(lit ? "已完成 1 / 1" : "待点亮 1 个", result.Message);
        Assert.Equal(2, medals.FreshReads);
        await using var db = await _factory.CreateDbContextAsync();
        Assert.Equal(
            expectedSuccess ? TaskRecordStatus.Success : TaskRecordStatus.Pending,
            Assert.Single(db.TaskRecords).Status
        );
    }

    private sealed class SuccessfulMedalTask : IAccountTaskAppService
    {
        public string TaskKey => "LiveFansMedalAppService";

        public Task DoTaskAsync(CancellationToken token = default) =>
            throw new NotSupportedException();

        public Task DoTaskForAccountAsync(long userId, CancellationToken token = default) =>
            Task.CompletedTask;
    }

    private sealed class ReadOnlyMedalResult(bool lit) : ILiveMedalDashboardService
    {
        public int FreshReads { get; private set; }

        public IReadOnlyList<LiveMedalAccount> GetAccounts() => throw new NotSupportedException();

        public Task<LiveMedalSnapshot?> GetCachedAsync(
            int index,
            CancellationToken token = default
        ) => throw new NotSupportedException();

        public Task<LiveMedalSnapshot> GetAsync(
            int index,
            bool refresh = false,
            CancellationToken token = default
        )
        {
            Assert.True(refresh);
            FreshReads++;
            return Task.FromResult(
                new LiveMedalSnapshot(
                    [
                        new(
                            1,
                            "示例主播",
                            "示例牌",
                            12,
                            false,
                            lit && FreshReads > 1,
                            false,
                            [new("sendDanmu", "弹幕", "仅点亮", false, null)],
                            null
                        ),
                    ],
                    DateTimeOffset.UtcNow
                )
            );
        }
    }

    private sealed class FailingRecoveryTask(string taskKey = "MangaTaskAppService")
        : IAccountTaskAppService
    {
        public string TaskKey => taskKey;

        public Task DoTaskAsync(CancellationToken token = default) =>
            throw new NotSupportedException();

        public async Task DoTaskForAccountAsync(long userId, CancellationToken token = default)
        {
            await Task.Yield();
            Assert.True(TaskFailureNotificationScope.IsSuppressed);
            throw new InvalidOperationException("synthetic recovery failure");
        }
    }

    private sealed class AllowGuard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken token = default
        ) => Task.CompletedTask;
    }

    private sealed class CaptureMonitor : ITaskFailureBatchMonitor
    {
        public List<string> TaskKeys { get; } = [];

        public IDisposable BeginBatch() => throw new NotSupportedException();

        public Task RecordFailureAsync(
            long? userId,
            string taskKey,
            CancellationToken token = default
        )
        {
            TaskKeys.Add(taskKey);
            return Task.CompletedTask;
        }

        public Task FlushReadyAsync(CancellationToken token = default) => Task.CompletedTask;
    }
}
