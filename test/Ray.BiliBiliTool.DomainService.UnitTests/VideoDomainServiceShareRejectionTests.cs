using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;
using Ray.BiliBiliTool.DomainService.Dtos;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

/// <summary>
/// 分享被 -403 拒绝时是账号级风控标记：只请求一次、不重试、不抛异常，跳过当日分享。
/// </summary>
public sealed class VideoDomainServiceShareRejectionTests
{
    [Fact]
    public async Task ShareVideo_Forbidden403_SkipsOnceWithoutRetrying()
    {
        var shareCalls = 0;
        var logger = new RecordingLogger<VideoDomainService>();
        var api = Proxy.Create<IApiApi>(
            (method, _) =>
            {
                if (method == nameof(IApiApi.ShareVideo))
                {
                    shareCalls++;
                    return Task.FromResult(
                        new BiliApiResponse { Code = -403, Message = "账号异常" }
                    );
                }

                throw new InvalidOperationException($"Unexpected API: {method}");
            }
        );
        var service = CreateService(logger, api);

        await service.ShareVideo(CreateVideo(), CreateCookie());

        Assert.Equal(1, shareCalls);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Warning && entry.Message.Contains("跳过当日分享")
        );
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ShareVideo_OtherFailure_KeepsExistingFailureHandling()
    {
        var shareCalls = 0;
        var logger = new RecordingLogger<VideoDomainService>();
        var api = Proxy.Create<IApiApi>(
            (method, _) =>
            {
                if (method == nameof(IApiApi.ShareVideo))
                {
                    shareCalls++;
                    return Task.FromResult(
                        new BiliApiResponse { Code = -412, Message = "请求被拦截" }
                    );
                }

                throw new InvalidOperationException($"Unexpected API: {method}");
            }
        );
        var service = CreateService(logger, api);

        await service.ShareVideo(CreateVideo(), CreateCookie());

        Assert.Equal(1, shareCalls);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error && entry.Message.Contains("视频分享失败")
        );
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("跳过当日分享"));
    }

    [Fact]
    public async Task ShareVideo_Success_LogsSuccess()
    {
        var logger = new RecordingLogger<VideoDomainService>();
        var api = Proxy.Create<IApiApi>(
            (method, _) =>
                method == nameof(IApiApi.ShareVideo)
                    ? Task.FromResult(new BiliApiResponse { Code = 0 })
                    : throw new InvalidOperationException($"Unexpected API: {method}")
        );
        var service = CreateService(logger, api);

        await service.ShareVideo(CreateVideo(), CreateCookie());

        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Information && entry.Message.Contains("视频分享成功")
        );
    }

    [Fact]
    public async Task WatchAndShareVideo_ShareForbidden403_CompletesWithoutThrowing()
    {
        var shareCalls = 0;
        var logger = new RecordingLogger<VideoDomainService>();
        var api = Proxy.Create<IApiApi>(
            (method, _) =>
                method switch
                {
                    nameof(IApiApi.GetFollowings) => Task.FromResult(
                        new BiliApiResponse<GetFollowingsResponse>
                        {
                            Code = 0,
                            Data = new GetFollowingsResponse(),
                        }
                    ),
                    nameof(IApiApi.GetRegionRankingVideosV2) => Task.FromResult(
                        new BiliApiResponse<Ranking>
                        {
                            Code = 0,
                            Data = new Ranking { List = [CreateRankingVideo()] },
                        }
                    ),
                    nameof(IApiApi.UploadVideoHeartbeat) => Task.FromResult(
                        new BiliApiResponse { Code = 0 }
                    ),
                    nameof(IApiApi.ShareVideo) => CountShare(),
                    _ => throw new InvalidOperationException($"Unexpected API: {method}"),
                }
        );
        var service = CreateService(
            new DailyTaskOptions { IsWatchVideo = true, IsShareVideo = true },
            logger,
            api
        );

        await service.WatchAndShareVideo(
            new DailyTaskInfo { Watch = true, Share = false },
            CreateCookie()
        );

        Assert.Equal(1, shareCalls);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Warning && entry.Message.Contains("跳过当日分享")
        );

        Task<BiliApiResponse> CountShare()
        {
            shareCalls++;
            return Task.FromResult(new BiliApiResponse { Code = -403, Message = "账号异常" });
        }
    }

    private static VideoDomainService CreateService(
        ILogger<VideoDomainService> logger,
        IApiApi api
    ) => CreateService(new DailyTaskOptions(), logger, api);

    private static VideoDomainService CreateService(
        DailyTaskOptions options,
        ILogger<VideoDomainService> logger,
        IApiApi api
    ) => new(logger, new StaticOptionsMonitor<DailyTaskOptions>(options), api);

    private static VideoInfoDto CreateVideo() =>
        new()
        {
            Aid = "123",
            Bvid = "BV1test",
            Title = "test",
        };

    private static RankingInfo CreateRankingVideo() =>
        new()
        {
            Aid = 123,
            Bvid = "BV1test",
            Cid = 1,
            Title = "test",
            Duration = 15,
        };

    private static BiliCookie CreateCookie() =>
        new(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = "123",
                ["SESSDATA"] = "synthetic",
                ["bili_jct"] = "synthetic",
                ["buvid3"] = "synthetic",
            }
        );

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Entries.Add((logLevel, formatter(state, exception)));
    }

    public class Proxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;
        public Action<string>? Before { get; set; }

        protected override object Invoke(MethodInfo? method, object?[]? args)
        {
            Before?.Invoke(method!.Name);
            return Handler(method!.Name, args!);
        }

        public static T Create<T>(
            Func<string, object?[], object> handler,
            Action<string>? before = null
        )
            where T : class
        {
            var value = Create<T, Proxy>();
            var proxy = (Proxy)(object)value;
            proxy.Handler = handler;
            proxy.Before = before;
            return value;
        }
    }
}
