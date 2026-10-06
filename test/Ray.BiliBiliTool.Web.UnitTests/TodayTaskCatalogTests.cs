using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Extensions;
using Ray.BiliBiliTool.Web.Jobs;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class TodayTaskCatalogTests
{
    [Fact]
    public async Task CatalogCoversEveryConfiguredSchedulerJobExactlyOnce()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddQuartz(q => q.AddBiliJobs(config))
            .BuildServiceProvider();
        var scheduler = await services.GetRequiredService<ISchedulerFactory>().GetScheduler();
        try
        {
            var jobs = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var scheduled = jobs.Where(key =>
                    !new[] { LoginJob.Key, TestBiliJob.Key, AutoRecoverJob.Key }.Contains(key)
                )
                .Select(key => key.Name)
                .Order()
                .ToArray();
            Assert.Equal(scheduled, TaskCatalog.All.Select(task => task.JobName).Order());
            Assert.Equal(10, TaskCatalog.All.Count);
            Assert.Equal(10, TaskCatalog.All.Select(task => task.TaskKey).Distinct().Count());
            Assert.Equal(
                10,
                TaskCatalog
                    .All.Select(task => TaskHelpCatalog.ConfigurationUrl(task.TaskKey))
                    .Distinct()
                    .Count()
            );
            Assert.All(TaskCatalog.All, task => Assert.NotNull(TaskHelpCatalog.Find(task.TaskKey)));
            Assert.DoesNotContain(
                TaskCatalog.All.Single(task => task.TaskKey == "DailyTaskAppService").Items,
                item => item.ItemKey == "VipPrivilege"
            );
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    [Theory]
    [InlineData("dailySuccessOnly", TodayTaskItemState.NotDone, 0)]
    [InlineData("dailyDisabled", TodayTaskItemState.NotDone, 0)]
    [InlineData("failed", TodayTaskItemState.Failed, 1)]
    [InlineData("success", TodayTaskItemState.Completed, 0)]
    [InlineData("future", TodayTaskItemState.Waiting, 0)]
    [InlineData("notToday", TodayTaskItemState.NotToday, 0)]
    public async Task WelfareStatusUsesItsOwnScheduleRecordsAndRecoveryCount(
        string scenario,
        TodayTaskItemState expected,
        int attempts
    )
    {
        var folder = Directory.CreateTempSubdirectory("today-catalog-");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sqlite"] =
                        $"Data Source={Path.Combine(folder.FullName, "test.db")}",
                    ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
                    ["DailyTaskConfig:IsEnable"] = (scenario != "dailyDisabled").ToString(),
                    ["VipPrivilegeConfig:IsEnable"] = "true",
                }
            )
            .Build();
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddDbContextFactory<BiliDbContext>()
            .AddQuartz()
            .BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<BiliDbContext>>();
        var schedulerFactory = services.GetRequiredService<ISchedulerFactory>();
        var scheduler = await schedulerFactory.GetScheduler();
        var clock = new LiveTaskTestSupport.BudgetClock
        {
            Now = new DateTimeOffset(2026, 10, 6, 4, 0, 0, TimeSpan.Zero),
        };
        var now = clock.GetLocalNow();
        var completionTime = clock.Now.AddMinutes(-3);
        try
        {
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                for (var attempt = 0; attempt < 3; attempt++)
                    db.TaskRecords.Add(
                        new()
                        {
                            UserId = 1001,
                            TaskKey = "DailyTaskAppService",
                            RecordDate = now.ToString("yyyy-MM-dd"),
                            Status = TaskRecordStatus.Success,
                            Trigger = TaskRecordTrigger.Auto,
                        }
                    );
                if (scenario is "failed" or "success")
                    db.TaskRecords.Add(
                        new()
                        {
                            UserId = 1001,
                            TaskKey = "VipPrivilegeTaskAppService",
                            RecordDate = now.ToString("yyyy-MM-dd"),
                            Status =
                                scenario == "success"
                                    ? TaskRecordStatus.Success
                                    : TaskRecordStatus.Failed,
                            Trigger =
                                scenario == "success"
                                    ? TaskRecordTrigger.Scheduled
                                    : TaskRecordTrigger.Auto,
                            CreatedAtUtc = completionTime,
                        }
                    );
                await db.SaveChangesAsync();
            }
            foreach (var task in TaskCatalog.All)
            {
                var cron =
                    task.TaskKey != "VipPrivilegeTaskAppService"
                        ? "0 0 0 * * ?"
                        : scenario switch
                        {
                            "future" => "0 59 23 * * ?",
                            "notToday" => "0 0 0 1 1 ? 2099",
                            _ => "0 0 0 * * ?",
                        };
                var key = new JobKey(task.JobName, Ray.BiliBiliTool.Web.Constants.BiliJobGroup);
                await scheduler.ScheduleJob(
                    JobBuilder.Create<TodayMedalStatusTests.StandbyJob>().WithIdentity(key).Build(),
                    TriggerBuilder
                        .Create()
                        .StartAt(new DateTimeOffset(now.Date.AddDays(-1), now.Offset))
                        .ForJob(key)
                        .WithCronSchedule(cron)
                        .Build()
                );
            }
            var workflow = DispatchProxy.Create<
                IBiliAccountPageWorkflow,
                TodayMedalStatusTests.TestProxy
            >();
            ((TodayMedalStatusTests.TestProxy)workflow).Call = (method, _) =>
                method.Name == "GetAllAccountsAsync"
                    ? Task.FromResult<List<BiliAccountDto>>([new(0, "1001", "synthetic")])
                    : throw new InvalidOperationException(method.Name);
            var today = new TodayTaskService(
                new(config),
                config,
                factory,
                schedulerFactory,
                null!,
                null!,
                workflow,
                null!,
                null!,
                null!,
                NullLogger<TodayTaskService>.Instance,
                null!,
                clock: clock
            );
            var account = Assert.Single(await today.GetTodayStatusAsync(false));
            Assert.Equal(
                TaskCatalog.All.Select(task => task.TaskKey),
                account.Groups.Select(group => group.TaskKey)
            );
            Assert.Equal(
                4,
                account.Groups.Single(group => group.TaskKey == "DailyTaskAppService").Items.Count
            );
            var welfare = Assert.Single(
                account.Groups.Single(group => group.TaskKey == "VipPrivilegeTaskAppService").Items
            );
            Assert.Null(welfare.ItemKey);
            Assert.False(welfare.IsBiliPending);
            Assert.Equal(expected, welfare.State);
            Assert.Equal(attempts, welfare.AutoAttempts);
            Assert.Equal(
                expected is TodayTaskItemState.NotDone or TodayTaskItemState.Failed,
                welfare.CanAutoRedo
            );
            if (scenario == "success")
                Assert.Equal(
                    completionTime.ToUnixTimeMilliseconds(),
                    welfare.CompletedAt!.Value.ToUnixTimeMilliseconds()
                );
            else
                Assert.Null(welfare.CompletedAt);
        }
        finally
        {
            await scheduler.Shutdown();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            folder.Delete(true);
        }
    }
}
