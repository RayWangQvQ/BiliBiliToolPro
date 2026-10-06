using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TodayMedalStatusTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TodayStatusUsesFreshMedalProgressAndLocalPhaseMakesNoPlatformCalls(
        bool includeBili,
        bool force
    )
    {
        var folder = Directory.CreateTempSubdirectory("today-medal-");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] =
                        $"Data Source={Path.Combine(folder.FullName, "test.db")}",
                    ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
                }
            )
            .Build();
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddDbContextFactory<BiliDbContext>()
            .AddQuartz()
            .BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<BiliDbContext>>();
        try
        {
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                db.TaskRecords.Add(
                    new()
                    {
                        UserId = 1001,
                        TaskKey = "LiveFansMedalAppService",
                        RecordDate = DateTimeOffset.Now.ToString("yyyy-MM-dd"),
                        Status = TaskRecordStatus.Success,
                        Trigger = TaskRecordTrigger.Auto,
                    }
                );
                await db.SaveChangesAsync();
            }
            var platformCalls = 0;
            var workflow = Proxy<IBiliAccountPageWorkflow>(
                (method, _) =>
                    method.Name == "GetAllAccountsAsync"
                        ? Task.FromResult<List<BiliAccountDto>>([new(0, "1001", "synthetic")])
                        : throw new InvalidOperationException(method.Name)
            );
            var schedulerFactory = services.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler();
            // Leave Quartz in standby so verification never executes activities.
            foreach (var task in TaskCatalog.All)
            {
                var key = new JobKey(task.JobName, Ray.BiliBiliTool.Web.Constants.BiliJobGroup);
                await scheduler.ScheduleJob(
                    JobBuilder.Create<StandbyJob>().WithIdentity(key).Build(),
                    TriggerBuilder
                        .Create()
                        .ForJob(key)
                        .WithCronSchedule(
                            task.JobName == "LiveFansMedalJob" ? "0 0 0 1 1 ? 2099" : "0 0 0 * * ?"
                        )
                        .Build()
                );
            }
            var account = Proxy<IAccountDomainService>(
                (_, _) =>
                {
                    platformCalls++;
                    return Task.FromResult(new DailyTaskInfo());
                }
            );
            var coin = Proxy<ICoinDomainService>(
                (_, _) =>
                {
                    platformCalls++;
                    return Task.FromResult(0);
                }
            );
            var probe = Proxy<IBiliAccountProbe>(
                (_, _) =>
                {
                    platformCalls++;
                    return Task.FromResult(
                        new Dictionary<long, BiliAccountProbeResult>
                        {
                            [1001] = new(true, "示例账号", 5, null, DateTimeOffset.UtcNow),
                        }
                    );
                }
            );
            var medalCalls = 0;
            var medals = Proxy<ILiveMedalDashboardService>(
                (method, args) =>
                {
                    Assert.Equal("GetAsync", method.Name);
                    Assert.Equal(0, args![0]);
                    Assert.Equal(force, args[1]);
                    medalCalls++;
                    return Task.FromResult(
                        new LiveMedalSnapshot(
                            [
                                new(
                                    1,
                                    "示例主播",
                                    "示例牌",
                                    12,
                                    false,
                                    false,
                                    false,
                                    [new("sendDanmu", "发送1条弹幕", "仅点亮", false, null)],
                                    null
                                ),
                            ],
                            DateTimeOffset.UtcNow
                        )
                    );
                }
            );
            var today = new TodayTaskService(
                new(config),
                config,
                factory,
                schedulerFactory,
                account,
                coin,
                workflow,
                probe,
                null!,
                null!,
                NullLogger<TodayTaskService>.Instance,
                medals
            );
            var result = await today.GetTodayStatusAsync(includeBili, force);
            var item = Assert
                .Single(result)
                .Groups.Single(group => group.TaskKey == "LiveFansMedalAppService")
                .Items.Single();
            Assert.NotEqual(TodayTaskItemState.Completed, item.State);
            Assert.Null(item.CompletedAt);
            Assert.False(item.CanAutoRedo);
            Assert.Equal(includeBili ? 1 : 0, medalCalls);
            if (!includeBili)
                Assert.Equal(0, platformCalls);
            if (TaskDueTimeCalculator.IsDue("0 0 0 * * ?", DateTimeOffset.Now))
            {
                Assert.Equal(!includeBili, item.IsBiliPending);
                Assert.Equal(includeBili ? "未完成" : "检测中", item.StateText);
                Assert.Equal(
                    includeBili ? "已完成 0 / 1 个粉丝牌 · 待点亮 1 个" : null,
                    item.ProgressSummary
                );
                Assert.Equal(includeBili, item.CanRedo);
            }
        }
        finally
        {
            await (
                await services.GetRequiredService<ISchedulerFactory>().GetScheduler()
            ).Shutdown();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            folder.Delete(true);
        }
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> call)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, TestProxy>();
        ((TestProxy)(object)proxy).Call = call;
        return proxy;
    }

    public class StandbyJob : IJob
    {
        public ValueTask Execute(IJobExecutionContext context, CancellationToken token = default) =>
            throw new InvalidOperationException("Standby verification must not execute activities");
    }

    public class TestProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Call { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            Call(method!, args);
    }
}
