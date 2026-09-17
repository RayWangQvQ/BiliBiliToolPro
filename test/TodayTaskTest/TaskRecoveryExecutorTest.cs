using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace TodayTaskTest;

public class TaskRecoveryExecutorTest
{
    private class FakeAccountTaskAppService : IAccountTaskAppService
    {
        public string TaskKey => "MangaTaskAppService";
        public long? RanFor { get; private set; }

        public Task DoTaskAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DoTaskForAccountAsync(
            long userId,
            CancellationToken cancellationToken = default
        )
        {
            RanFor = userId;
            return Task.CompletedTask;
        }
    }

    private static IConfiguration BuildConfig(string cookie) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["BiliBiliCookies:0"] = cookie }
            )
            .Build();

    private static TaskRecoveryExecutor BuildExecutor(
        IConfiguration config,
        FakeAccountTaskAppService fake
    )
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAccountTaskAppService>(fake);

        return new TaskRecoveryExecutor(
            new CookieStrFactory<BiliCookie>(config),
            config,
            null!,
            null!,
            null!,
            null!,
            services.BuildServiceProvider(),
            NullLogger<TaskRecoveryExecutor>.Instance
        );
    }

    [Fact]
    public async Task 任务级补做会调用到对应的AppService()
    {
        var config = BuildConfig("DedeUserID=1001; bili_jct=abc; SESSDATA=def");
        var fake = new FakeAccountTaskAppService();
        var executor = BuildExecutor(config, fake);
        var task = TaskCatalog.All.Single(t => t.TaskKey == "MangaTaskAppService");

        await executor.ExecuteAsync(1001, task, task.Items[0]);

        Assert.Equal(1001, fake.RanFor);
    }

    [Fact]
    public async Task 账号不存在时抛异常()
    {
        var config = BuildConfig("DedeUserID=1001; bili_jct=abc; SESSDATA=def");
        var fake = new FakeAccountTaskAppService();
        var executor = BuildExecutor(config, fake);
        var task = TaskCatalog.All.Single(t => t.TaskKey == "MangaTaskAppService");

        await Assert.ThrowsAsync<Exception>(() => executor.ExecuteAsync(9999, task, task.Items[0]));
    }

    [Fact]
    public async Task 未注册的任务服务抛异常()
    {
        var config = BuildConfig("DedeUserID=1001; bili_jct=abc; SESSDATA=def");
        var fake = new FakeAccountTaskAppService();
        var executor = BuildExecutor(config, fake);
        // 目录里存在但没往容器里注册对应实现
        var task = TaskCatalog.All.Single(t => t.TaskKey == "ChargeTaskAppService");

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            executor.ExecuteAsync(1001, task, task.Items[0])
        );
        Assert.Contains("未注册任务服务", ex.Message);
    }
}
