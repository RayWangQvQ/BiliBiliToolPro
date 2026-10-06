using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService;
using Refit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class TaskOutcomeTests
{
    private static BiliCookie Cookie => new(new() { ["bili_jct"] = "test-csrf" });

    [Theory]
    [InlineData(0, 100, 100, true, true, 7)]
    [InlineData(5, 0, 100, true, true, 7)]
    [InlineData(5, 0, 2, false, false, 8)]
    [InlineData(1, 0, 0, false, false, 7)]
    [InlineData(5, 7.9, 7, false, false, 7)]
    [InlineData(0, 0, 0, false, false, 20)]
    public void UpgradeForecast_RespectsProtectionAndEnabledTasks(
        int coins,
        double balance,
        int protectedCoins,
        bool watch,
        bool share,
        int expected
    )
    {
        var service = new AccountDomainService(
            new CaptureLogger<AccountDomainService>(),
            null!,
            null!,
            new TestOptions<UnfollowBatchedTaskOptions>(new()),
            new TestOptions<DailyTaskOptions>(
                new()
                {
                    NumberOfCoins = coins,
                    NumberOfProtectedCoins = protectedCoins,
                    IsWatchVideo = watch,
                    IsShareVideo = share,
                }
            )
        );
        Assert.Equal(
            expected,
            service.CalculateUpgradeTime(
                new UserInfo
                {
                    Money = (decimal)balance,
                    Level_info = new()
                    {
                        Current_level = 5,
                        Current_exp = 0,
                        Next_exp = 100,
                    },
                    Wbi_img = new() { img_url = "", sub_url = "" },
                }
            )
        );
    }

    [Fact]
    public void UpgradeForecast_MaximumLevel_HasNoNextLevel()
    {
        var service = new AccountDomainService(
            new CaptureLogger<AccountDomainService>(),
            null!,
            null!,
            new TestOptions<UnfollowBatchedTaskOptions>(new()),
            new TestOptions<DailyTaskOptions>(new())
        );
        Assert.Equal(
            0,
            service.CalculateUpgradeTime(
                new UserInfo
                {
                    Level_info = new() { Current_level = 6, Next_exp = "--" },
                    Wbi_img = new() { img_url = "", sub_url = "" },
                }
            )
        );
    }

    [Fact]
    public async Task MangaSign_NetworkFailure_PropagatesWithoutClaimingAlreadySigned()
    {
        var failure = new HttpRequestException("test network failure");
        var logger = new CaptureLogger<MangaDomainService>();
        var service = Manga(
            ApiStub.Create<IMangaApi>((_, _) => Task.FromException<BiliApiResponse>(failure)),
            logger
        );
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<HttpRequestException>(() => service.MangaSign(Cookie))
        );
        Assert.DoesNotContain(logger.Messages, message => message.Contains("已签到"));
    }

    [Theory]
    [InlineData("{\"code\":\"invalid_argument\",\"msg\":\"clockin clockin is duplicate\"}", true)]
    [InlineData("{\"code\":\"invalid_argument\",\"msg\":\"invalid platform\"}", false)]
    [InlineData("bad gateway", false)]
    [InlineData("[]", false)]
    public async Task MangaSign_OnlyRecognizesExplicitDuplicate(string body, bool duplicate)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://example.invalid/clockin"
        );
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body),
        };
        var failure = await ApiException.Create(
            request,
            HttpMethod.Post,
            response,
            new RefitSettings()
        );
        var logger = new CaptureLogger<MangaDomainService>();
        var service = Manga(
            ApiStub.Create<IMangaApi>((_, _) => Task.FromException<BiliApiResponse>(failure)),
            logger
        );
        if (duplicate)
        {
            await service.MangaSign(Cookie);
            Assert.Contains(logger.Messages, message => message.Contains("今日已签到"));
        }
        else
            Assert.Same(
                failure,
                await Assert.ThrowsAsync<ApiException>(() => service.MangaSign(Cookie))
            );
    }

    [Fact]
    public async Task MangaRead_Acknowledgment_ReportsRecordSubmissionOnly()
    {
        var logger = new CaptureLogger<MangaDomainService>();
        var service = Manga(
            ApiStub.Create<IMangaApi>((_, _) => Task.FromResult(new BiliApiResponse { Code = 0 })),
            logger
        );
        await service.MangaRead(Cookie);
        Assert.Contains(logger.Messages, message => message.Contains("阅读记录已提交"));
        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains("成功") || message.Contains("已完成")
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Manga_BusinessFailure_IsNotSuccessful(bool sign)
    {
        var service = Manga(
            ApiStub.Create<IMangaApi>(
                (_, _) => Task.FromResult(new BiliApiResponse { Code = -101 })
            ),
            new()
        );
        await Assert.ThrowsAsync<BiliBusinessException>(() =>
            sign ? service.MangaSign(Cookie) : service.MangaRead(Cookie)
        );
    }

    [Fact]
    public async Task MangaRead_DisabledComic_DoesNotCallApi()
    {
        var logger = new CaptureLogger<MangaDomainService>();
        var service = Manga(
            ApiStub.Create<IMangaApi>(
                (_, _) => throw new InvalidOperationException("Must not call")
            ),
            logger,
            0
        );
        await service.MangaRead(Cookie);
        Assert.Contains(logger.Messages, message => message.Contains("跳过"));
    }

    [Fact]
    public async Task VipReceive_Failure_StopsCompletionAndPreservesCode()
    {
        var api = ApiStub.Create<IApiApi>(
            (name, args) =>
            {
                Assert.Equal(nameof(IApiApi.VipBigPointReceiveV2), name);
                Assert.Equal("test-csrf", ((VipPointV2TaskRequest)args![0]!).Csrf);
                return Task.FromResult(new BiliApiResponse { Code = 6007000 });
            }
        );
        var service = Vip(api);
        var completed = false;
        var error = await Assert.ThrowsAsync<BiliBusinessException>(() =>
            service.ReceiveAndCompleteAsync(
                Combine(0),
                "日常任务",
                "dress-view",
                Cookie,
                (_, _) =>
                {
                    completed = true;
                    return Task.FromResult(true);
                }
            )
        );
        Assert.Contains("6007000", error.Message);
        Assert.False(completed);
    }

    [Fact]
    public async Task VipReceive_BatchFailure_ContinuesOtherMissionsThenFails()
    {
        var calls = new List<string>();
        var api = ApiStub.Create<IApiApi>(
            (_, args) =>
            {
                var code = ((ReceiveOrCompleteTaskRequest)args![0]!).TaskCode;
                calls.Add(code);
                return Task.FromResult(
                    new BiliApiResponse { Code = code == "dress-view" ? 6007000 : 0 }
                );
            }
        );
        var info = Combine(0);
        info.Task_info.Modules[0]
            .common_task_item.Add(
                new()
                {
                    title = "Second",
                    task_code = "second",
                    state = 0,
                }
            );
        var error = await Assert.ThrowsAsync<AggregateException>(() =>
            Vip(api).ReceiveDailyMissionsAsync(info, Cookie)
        );
        Assert.Single(error.InnerExceptions);
        Assert.Equal(new[] { "dress-view", "second" }, calls);
    }

    [Theory]
    [InlineData(1, 0, false)]
    [InlineData(3, 0, false)]
    [InlineData(3, 1, true)]
    public async Task VipComplete_RequiresConfirmedTaskState(int state, int times, bool success)
    {
        var confirmed = Combine(state);
        confirmed.Task_info.Modules[0].common_task_item[0].complete_times = times;
        var api = ApiStub.Create<IApiApi>(
            (name, _) =>
            {
                Assert.Equal(nameof(IApiApi.GetCombineAsync), name);
                return Task.FromResult(
                    new BiliApiResponse<VipBigPointCombine> { Code = 0, Data = confirmed }
                );
            }
        );
        var action = () =>
            Vip(api)
                .ReceiveAndCompleteAsync(
                    Combine(1),
                    "日常任务",
                    "dress-view",
                    Cookie,
                    (_, _) => Task.FromResult(true)
                );
        if (success)
            await action();
        else
            await Assert.ThrowsAsync<BiliBusinessException>(action);
    }

    private static MangaDomainService Manga(
        IMangaApi api,
        CaptureLogger<MangaDomainService> logger,
        long comic = 1
    ) =>
        new(
            logger,
            api,
            new TestOptions<MangaTaskOptions>(new() { CustomComicId = comic }),
            new TestOptions<DailyTaskOptions>(new()),
            new TestOptions<VipPrivilegeOptions>(new())
        );

    private static VipBigPointDomainService Vip(IApiApi api) =>
        new(
            new CaptureLogger<VipBigPointDomainService>(),
            new TestOptions<VipBigPointOptions>(new()),
            null!,
            api,
            null!,
            null!
        );

    private static VipBigPointCombine Combine(int state) =>
        new()
        {
            point_info = new(0, 0, 0, 0),
            Task_info = new()
            {
                Sing_task_item = new(),
                Modules =
                [
                    new()
                    {
                        module_title = "日常任务",
                        common_task_item =
                        [
                            new()
                            {
                                title = "Test",
                                task_code = "dress-view",
                                state = state,
                            },
                        ],
                    },
                ],
            },
        };
}

public class ApiStub : DispatchProxy
{
    private Func<string, object?[]?, object?> _invoke = null!;

    public static T Create<T>(Func<string, object?[]?, object?> invoke)
        where T : class
    {
        var proxy = Create<T, ApiStub>();
        ((ApiStub)(object)proxy)._invoke = invoke;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        _invoke(targetMethod!.Name, args);
}

internal sealed class TestOptions<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class CaptureLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel level) => true;

    public void Log<TState>(
        LogLevel level,
        EventId id,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) => Messages.Add(formatter(state, exception));
}
