using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Application.Contracts.Cookies;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class VideoRecoveryOutcomeTests
{
    [Theory]
    [InlineData("Watch")]
    [InlineData("Share")]
    public async Task RejectedVideoActionReturnsReasonAndWritesFailedRecord(string item)
    {
        var environment = new Environment(item, -403, false);
        using var services = new ServiceCollection().BuildServiceProvider();
        var today = environment.Build(services);
        var entries = new List<TaskRecoveryProgress>();
        using var scope = new TaskRecoveryProgressScope(entries.Add);
        var result = await today.RedoAsync(1001, "DailyTaskAppService", item);
        Assert.False(result.Success);
        Assert.Contains("-403", result.Message);
        Assert.Contains("synthetic rejection", result.Message);
        Assert.Equal(TaskRecordStatus.Failed, Assert.Single(environment.Writer.Statuses));
        Assert.Equal(TaskRecoveryProgressState.Failed, entries.Last().State);
        Assert.DoesNotContain("执行完成", result.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptedVideoActionUsesPlatformConfirmationForCompletion(bool confirmed)
    {
        var environment = new Environment("Watch", 0, confirmed);
        using var services = new ServiceCollection().BuildServiceProvider();
        var result = await environment
            .Build(services)
            .RedoAsync(1001, "DailyTaskAppService", "Watch");
        Assert.Equal(confirmed, result.Success);
        Assert.Equal(
            confirmed ? TaskRecordStatus.Success : TaskRecordStatus.Pending,
            Assert.Single(environment.Writer.Statuses)
        );
        Assert.Contains(confirmed ? "B 站已确认完成" : "B 站尚未确认完成", result.Message);
    }

    private sealed class Environment
    {
        private readonly IConfiguration _config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["BiliBiliCookies:0"] = "DedeUserID=1001;bili_jct=synthetic;SESSDATA=synthetic",
                }
            )
            .Build();
        private readonly IVideoDomainService _video;
        private readonly IAccountDomainService _account;
        private readonly ICoinDomainService _coin;
        public Writer Writer { get; } = new();

        public Environment(string item, int code, bool confirmed)
        {
            var api = DispatchProxy.Create<IApiApi, Proxy>();
            var uploads = 0;
            ((Proxy)api).Call = method =>
                method.Name switch
                {
                    "ShareVideo" => Task.FromResult(
                        new BiliApiResponse { Code = code, Message = "synthetic rejection" }
                    ),
                    "UploadVideoHeartbeat" => Task.FromResult(
                        new BiliApiResponse
                        {
                            Code = ++uploads == 1 ? 0 : code,
                            Message = "synthetic rejection",
                        }
                    ),
                    _ => throw new NotSupportedException(method.Name),
                };
            var real = new VideoDomainService(
                NullLogger<VideoDomainService>.Instance,
                new Monitor<DailyTaskOptions>(new()),
                api
            );
            var cookie = new CookieStrFactory<BiliCookie>(_config).GetCookie(0);
            var video = new VideoInfoDto
            {
                Aid = "1",
                Bvid = "synthetic",
                Title = "synthetic",
            };
            _video = DispatchProxy.Create<IVideoDomainService, Proxy>();
            ((Proxy)_video).Call = method =>
                method.Name switch
                {
                    "GetRandomVideoForWatchAndShare" => Task.FromResult(video),
                    "OpenVideo" => Task.FromResult(true),
                    "ShareVideo" => real.ShareVideo(video, cookie),
                    "WatchVideo" => real.WatchVideo(video, cookie),
                    _ => throw new NotSupportedException(method.Name),
                };
            _account = DispatchProxy.Create<IAccountDomainService, Proxy>();
            var reads = 0;
            ((Proxy)_account).Call = _ =>
                Task.FromResult(new DailyTaskInfo { Watch = ++reads > 1 && confirmed });
            _coin = DispatchProxy.Create<ICoinDomainService, Proxy>();
            ((Proxy)_coin).Call = _ => Task.FromResult(0);
        }

        public TodayTaskService Build(IServiceProvider services)
        {
            var cookies = new CookieStrFactory<BiliCookie>(_config);
            var executor = new TaskRecoveryExecutor(
                cookies,
                _config,
                _account,
                _video,
                null!,
                null!,
                services,
                NullLogger<TaskRecoveryExecutor>.Instance,
                new Guard()
            );
            return new TodayTaskService(
                cookies,
                _config,
                null!,
                null!,
                _account,
                _coin,
                null!,
                null!,
                executor,
                Writer,
                NullLogger<TodayTaskService>.Instance,
                null!
            );
        }
    }

    private sealed class Guard : ICookieTaskGuard
    {
        public Task EnsureValidAsync(
            string userId,
            string cookie,
            CancellationToken token = default
        ) => Task.CompletedTask;
    }

    private sealed class Monitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    public sealed class Writer : ITaskRecordWriter
    {
        public List<TaskRecordStatus> Statuses { get; } = [];

        public Task WriteAsync(
            long userId,
            string taskKey,
            string? itemKey,
            TaskRecordStatus status,
            string? message,
            TaskRecordTrigger trigger,
            CancellationToken cancellationToken = default
        )
        {
            Statuses.Add(status);
            return Task.CompletedTask;
        }
    }

    public class Proxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Call { get; set; } = null!;

        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!);
    }
}
