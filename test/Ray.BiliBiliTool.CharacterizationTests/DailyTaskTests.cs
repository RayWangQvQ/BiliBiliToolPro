using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Daily;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Video;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.NavApi;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.CharacterizationTests.Support;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.CharacterizationTests;

[Collection("Characterization")]
public class DailyTaskTests
{
    [Fact]
    public async Task DoTaskAsync_EnabledTasks_PreservesSequenceAndDiagnostics()
    {
        var callLog = new List<string>();
        using var logging = TestLoggingContext.Create();

        var configuration = BuildConfiguration(platformType: "Web", CreateCookieString("201"));
        var service = CreateService(
            configuration,
            logging,
            callLog,
            new AccountDomainServiceDouble(callLog),
            new VideoDomainServiceDouble(callLog),
            new ArticleDomainServiceDouble(callLog),
            new DonateCoinDomainServiceDouble(callLog),
            new VipPrivilegeDomainServiceDouble(callLog),
            new LoginDomainServiceDouble(callLog),
            new DailyTaskOptions
            {
                IsEnable = true,
                IsWatchVideo = true,
                IsShareVideo = true,
            }
        );

        await service.DoTaskAsync();

        callLog
            .Should()
            .Equal(
                "SetCookieAsync",
                "SaveCookieToJsonFileAsync",
                "LoginByCookie#1",
                "GetDailyTaskStatus",
                "WatchAndShareVideo",
                "AddCoinsForVideos",
                "ReceiveVipPrivilege"
            );

        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowStart DailyTask"));
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowCompleted DailyTask"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("每日任务"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("登录"));
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("观看、分享视频"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("投币"));
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("领取大会员福利"));
    }

    [Fact]
    public async Task DoTaskAsync_FirstAccountFails_ContinuesWithNextAccount()
    {
        var callLog = new List<string>();
        using var logging = TestLoggingContext.Create();

        var configuration = BuildConfiguration(
            platformType: "Web",
            CreateCookieString("301", includeBuvid: true),
            CreateCookieString("302", includeBuvid: true)
        );
        var accountDomainService = new AccountDomainServiceDouble(callLog, throwOnLoginCall: 1);
        var service = CreateService(
            configuration,
            logging,
            callLog,
            accountDomainService,
            new VideoDomainServiceDouble(callLog),
            new ArticleDomainServiceDouble(callLog),
            new DonateCoinDomainServiceDouble(callLog),
            new VipPrivilegeDomainServiceDouble(callLog),
            new LoginDomainServiceDouble(callLog),
            new DailyTaskOptions
            {
                IsEnable = true,
                IsWatchVideo = true,
                IsShareVideo = true,
            }
        );

        await service.DoTaskAsync();

        accountDomainService.LoginByCookieCallCount.Should().Be(2);
        callLog.Should().Contain("LoginByCookie#1");
        callLog.Should().Contain("LoginByCookie#2");
        callLog.Should().Contain("WatchAndShareVideo");
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowFailed DailyTask"));
        logging
            .Collector.Entries.Should()
            .Contain(entry => entry.Message.Contains("FlowCompleted DailyTask"));
        logging.Collector.Entries.Should().Contain(entry => entry.Message.Contains("异常："));
    }

    [Fact]
    public async Task Suppressed_check_failure_is_recorded_and_does_not_contaminate_next_account()
    {
        var calls = new List<string>();
        var writer = new CaptureWriter();
        using var logging = TestLoggingContext.Create(writer);
        var configuration = BuildConfiguration(
            "Web",
            CreateCookieString("301", true),
            CreateCookieString("302", true)
        );
        var service = CreateService(
            configuration,
            logging,
            calls,
            new AccountDomainServiceDouble(calls),
            new VideoDomainServiceDouble(calls, failFirst: true),
            new ArticleDomainServiceDouble(calls),
            new DonateCoinDomainServiceDouble(calls),
            new VipPrivilegeDomainServiceDouble(calls),
            new LoginDomainServiceDouble(calls),
            new DailyTaskOptions
            {
                IsEnable = true,
                IsWatchVideo = true,
                IsShareVideo = true,
            }
        );
        await service.DoTaskAsync();
        Assert.Equal(new[] { TaskRecordStatus.Failed, TaskRecordStatus.Success }, writer.Statuses);
        Assert.Equal(2, calls.Count(call => call == "AddCoinsForVideos"));
    }

    [Fact]
    public async Task Suppressed_check_failure_in_manual_recovery_is_not_reported_as_success()
    {
        var calls = new List<string>();
        using var logging = TestLoggingContext.Create();
        var configuration = BuildConfiguration("Web", CreateCookieString("301", true));
        var service = CreateService(
            configuration,
            logging,
            calls,
            new AccountDomainServiceDouble(calls),
            new VideoDomainServiceDouble(calls, failFirst: true),
            new ArticleDomainServiceDouble(calls),
            new DonateCoinDomainServiceDouble(calls),
            new VipPrivilegeDomainServiceDouble(calls),
            new LoginDomainServiceDouble(calls),
            new DailyTaskOptions
            {
                IsEnable = true,
                IsWatchVideo = true,
                IsShareVideo = true,
            }
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DoTaskForAccountAsync(301)
        );
        Assert.Contains("AddCoinsForVideos", calls);
    }

    private sealed class CaptureWriter : ITaskRecordWriter
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

    [Theory]
    [InlineData(3, false, true)]
    [InlineData(4, false, false)]
    [InlineData(6, false, false)]
    [InlineData(3, true, true)]
    [InlineData(4, true, false)]
    public async Task ConfiguredLevelThreshold_AppliesToBothDonationModes(
        int level,
        bool articles,
        bool shouldDonate
    )
    {
        var calls = new List<string>();
        using var logging = TestLoggingContext.Create();
        var service = CreateService(
            BuildConfiguration("Web", CreateCookieString("201")),
            logging,
            calls,
            new AccountDomainServiceDouble(calls, level: level),
            new VideoDomainServiceDouble(calls),
            new ArticleDomainServiceDouble(calls),
            new DonateCoinDomainServiceDouble(calls),
            new VipPrivilegeDomainServiceDouble(calls),
            new LoginDomainServiceDouble(calls),
            new DailyTaskOptions { CoinDonationStopLevel = 4, IsDonateCoinForArticle = articles }
        );
        await service.DoTaskAsync();
        var donated = calls.Contains("AddCoinsForVideos") || calls.Contains("AddCoinForArticles");
        donated.Should().Be(shouldDonate);
        calls.Should().Contain("ReceiveVipPrivilege");
    }

    private static Ray.BiliBiliTool.Application.DailyTaskAppService CreateService(
        IConfiguration configuration,
        TestLoggingContext logging,
        List<string> callLog,
        IAccountDomainService accountDomainService,
        IVideoDomainService videoDomainService,
        IArticleDomainService articleDomainService,
        IDonateCoinDomainService donateCoinDomainService,
        IVipPrivilegeDomainService vipPrivilegeDomainService,
        ILoginDomainService loginDomainService,
        DailyTaskOptions options
    )
    {
        return new Ray.BiliBiliTool.Application.DailyTaskAppService(
            logging.LoggerFactory.CreateLogger<Ray.BiliBiliTool.Application.DailyTaskAppService>(),
            accountDomainService,
            videoDomainService,
            articleDomainService,
            donateCoinDomainService,
            vipPrivilegeDomainService,
            new StaticOptionsMonitor<DailyTaskOptions>(options),
            loginDomainService,
            configuration,
            new CookieStrFactory<BiliCookie>(configuration),
            new AllowCookieTaskGuard()
        );
    }

    private static IConfiguration BuildConfiguration(string platformType, params string[] cookies)
    {
        var values = new Dictionary<string, string?> { ["PlatformType"] = platformType };

        for (int i = 0; i < cookies.Length; i++)
        {
            values[$"BiliBiliCookies:{i}"] = cookies[i];
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static string CreateCookieString(string userId, bool includeBuvid = false)
    {
        var parts = new List<string>
        {
            $"DedeUserID={userId}",
            $"SESSDATA=sess-{userId}",
            $"bili_jct=jct-{userId}",
        };

        if (includeBuvid)
        {
            parts.Add($"buvid3=buvid-{userId}");
        }

        return string.Join("; ", parts);
    }

    private static UserInfo CreateUserInfo(int level = 1)
    {
        return new UserInfo
        {
            IsLogin = true,
            Mid = 1,
            Level_info = new LevelInfo
            {
                Current_level = level,
                Current_exp = 0,
                Next_exp = 100L,
            },
            Wbi_img = new WbiImg
            {
                img_url = "https://example.com/wbi/test.png",
                sub_url = "https://example.com/wbi/sub.png",
            },
        };
    }

    private sealed class AccountDomainServiceDouble(
        List<string> callLog,
        int? throwOnLoginCall = null,
        int level = 1
    ) : IAccountDomainService
    {
        public int LoginByCookieCallCount { get; private set; }

        public Task<UserInfo> LoginByCookie(BiliCookie cookie)
        {
            LoginByCookieCallCount++;
            callLog.Add($"LoginByCookie#{LoginByCookieCallCount}");

            if (throwOnLoginCall == LoginByCookieCallCount)
            {
                throw new InvalidOperationException("login failure");
            }

            return Task.FromResult(CreateUserInfo(level));
        }

        public Task<DailyTaskInfo> GetDailyTaskStatus(BiliCookie ck)
        {
            callLog.Add(nameof(GetDailyTaskStatus));
            return Task.FromResult(new DailyTaskInfo());
        }

        public Task UnfollowBatched(BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public int CalculateUpgradeTime(UserInfo useInfo)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class VideoDomainServiceDouble(List<string> callLog, bool failFirst = false)
        : IVideoDomainService
    {
        private int _watchCalls;

        public Task<VideoDetail> GetVideoDetail(string aid)
        {
            throw new NotSupportedException();
        }

        public Task<RankingInfo> GetRandomVideoOfRanking()
        {
            throw new NotSupportedException();
        }

        public Task<UpVideoInfo?> GetRandomVideoOfUp(long upId, int total, BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetVideoCountOfUp(long upId, BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task WatchAndShareVideo(DailyTaskInfo dailyTaskStatus, BiliCookie ck)
        {
            callLog.Add(nameof(WatchAndShareVideo));
            if (failFirst && ++_watchCalls == 1)
                throw new InvalidOperationException("synthetic check failure");
            return Task.CompletedTask;
        }

        public Task WatchVideo(VideoInfoDto videoInfo, BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task ShareVideo(VideoInfoDto videoInfo, BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task<VideoInfoDto> GetRandomVideoForWatchAndShare(BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task<bool> OpenVideo(VideoInfoDto videoInfo, BiliCookie ck)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ArticleDomainServiceDouble(List<string> callLog) : IArticleDomainService
    {
        public Task<bool> AddCoinForArticle(long cvid, long mid, BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task<bool> AddCoinForArticles(BiliCookie ck)
        {
            callLog.Add(nameof(AddCoinForArticles));
            return Task.FromResult(false);
        }

        public Task LikeArticle(long cvid, BiliCookie ck)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class DonateCoinDomainServiceDouble(List<string> callLog)
        : IDonateCoinDomainService
    {
        public Task AddCoinsForVideos(BiliCookie ck)
        {
            callLog.Add(nameof(AddCoinsForVideos));
            return Task.CompletedTask;
        }

        public Task<UpVideoInfo?> TryGetCanDonatedVideo(BiliCookie ck)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DoAddCoinForVideo(UpVideoInfo video, bool select_like, BiliCookie ck)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class VipPrivilegeDomainServiceDouble(List<string> callLog)
        : IVipPrivilegeDomainService
    {
        public Task<bool> ReceiveVipPrivilege(UserInfo userInfo, BiliCookie ck)
        {
            callLog.Add(nameof(ReceiveVipPrivilege));
            return Task.FromResult(false);
        }
    }

    private sealed class LoginDomainServiceDouble(List<string> callLog) : ILoginDomainService
    {
        public Task<BiliCookie> LoginByQrCodeAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<BiliCookie> SetCookieAsync(
            BiliCookie cookie,
            CancellationToken cancellationToken
        )
        {
            callLog.Add(nameof(SetCookieAsync));
            return Task.FromResult(
                new BiliCookie(
                    new Dictionary<string, string>
                    {
                        ["DedeUserID"] = cookie.UserId,
                        ["SESSDATA"] = cookie.SessData,
                        ["bili_jct"] = cookie.BiliJct,
                        ["buvid3"] = "generated-buvid",
                    }
                )
            );
        }

        public Task SaveCookieToJsonFileAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            callLog.Add(nameof(SaveCookieToJsonFileAsync));
            return Task.CompletedTask;
        }

        public Task<bool> SaveCookieToQinLongAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<bool> SaveCookieToBaihuAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<bool> SaveCookieToDaiDaiAsync(
            BiliCookie ckInfo,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<QrLoginGenerateResult> GenerateQrCodeWebAsync(
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public Task<QrLoginCheckResult> CheckQrLoginAsync(
            string qrcodeKey,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
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

    private sealed class TestLoggingContext(
        IServiceProvider serviceProvider,
        TestLogCollector collector
    ) : IDisposable
    {
        public ILoggerFactory LoggerFactory => serviceProvider.GetRequiredService<ILoggerFactory>();

        public TestLogCollector Collector { get; } = collector;

        public static TestLoggingContext Create(ITaskRecordWriter? writer = null)
        {
            var collector = new TestLogCollector();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.ClearProviders().AddProvider(collector));
            if (writer is not null)
                services.AddSingleton(writer);

            var serviceProvider = services.BuildServiceProvider();
            Global.ServiceProviderRoot = serviceProvider;

            return new TestLoggingContext(serviceProvider, collector);
        }

        public void Dispose()
        {
            Global.ServiceProviderRoot = null;
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
