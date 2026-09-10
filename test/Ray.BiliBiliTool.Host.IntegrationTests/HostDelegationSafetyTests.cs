using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Web.Jobs;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

public class HostDelegationSafetyTests
{
    [Fact]
    public async Task Console_hosted_service_delegates_selected_tasks_to_application_services()
    {
        var loginTask = new RecordingAppService();
        var dailyTask = new RecordingAppService();
        var services = new ServiceCollection();
        services.AddSingleton<ILoginTaskAppService>(loginTask);
        services.AddSingleton<IDailyTaskAppService>(dailyTask);

        using var serviceProvider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["RunTasks"] = "Login&Daily" })
            .Build();

        var hostedService = new BiliBiliToolHostedService(
            new RecordingHostApplicationLifetime(),
            serviceProvider,
            new FakeHostEnvironment(),
            configuration,
            NullLogger<BiliBiliToolHostedService>.Instance,
            new StaticOptionsMonitor<SecurityOptions>(new SecurityOptions { RandomSleepMaxMin = 0 })
        );

        await hostedService.StartAsync(CancellationToken.None);

        loginTask.CallCount.Should().Be(1);
        dailyTask.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Login_job_delegates_to_login_task_app_service()
    {
        var appService = new RecordingAppService();
        var job = new LoginJob(NullLogger<LoginJob>.Instance, appService);

        await InvokeJobDelegateAsync(job);

        appService.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Daily_job_delegates_to_daily_task_app_service()
    {
        var appService = new RecordingAppService();
        var job = new DailyJob(NullLogger<DailyJob>.Instance, appService);

        await InvokeJobDelegateAsync(job);

        appService.CallCount.Should().Be(1);
    }

    private static async Task InvokeJobDelegateAsync(object job)
    {
        var method = job.GetType()
            .GetMethod("DoExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();

        var task = (Task?)method!.Invoke(job, [null]);
        task.Should().NotBeNull();
        await task!;
    }

    private sealed class RecordingAppService : ILoginTaskAppService, IDailyTaskAppService
    {
        public int CallCount { get; private set; }

        public Task DoTaskAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener)
        {
            return null;
        }
    }

    private sealed class RecordingHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() { }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = nameof(BiliBiliToolHostedService);

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
