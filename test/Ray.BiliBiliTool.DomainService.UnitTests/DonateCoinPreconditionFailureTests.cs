using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

/// <summary>
/// 投币前的「今日已投数」「硬币余额」查询失败时降级为跳过本次投币，不得抛异常中断账号其余任务。
/// </summary>
public sealed class DonateCoinPreconditionFailureTests
{
    [Fact]
    public async Task AddCoinsForVideos_DonatedCoinsQueryFails_DegradesToSkipWithoutThrowing()
    {
        var coinDomainService = new StubCoinDomainService
        {
            DonatedCoinsException = new InvalidOperationException("获取投币经验失败：接口超时"),
        };
        var logger = new RecordingLogger<DonateCoinDomainService>();
        var service = CreateService(logger, coinDomainService);

        var exception = await Record.ExceptionAsync(() =>
            service.AddCoinsForVideos(CreateCookie())
        );

        Assert.Null(exception);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == LogLevel.Warning && entry.Message.Contains("获取今日已投币数失败")
        );
    }

    [Fact]
    public async Task AddCoinsForVideos_CoinBalanceQueryFails_DegradesToSkipWithoutThrowing()
    {
        var coinDomainService = new StubCoinDomainService
        {
            DonatedCoins = 0,
            CoinBalanceException = new InvalidOperationException("获取硬币余额失败：接口超时"),
        };
        var logger = new RecordingLogger<DonateCoinDomainService>();
        var service = CreateService(logger, coinDomainService);

        var exception = await Record.ExceptionAsync(() =>
            service.AddCoinsForVideos(CreateCookie())
        );

        Assert.Null(exception);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Warning && entry.Message.Contains("获取硬币余额失败")
        );
    }

    [Fact]
    public async Task AddCoinsForVideos_QuerySucceeds_DoesNotEnterFailurePath()
    {
        var coinDomainService = new StubCoinDomainService { DonatedCoins = 0, CoinBalance = 0m };
        var logger = new RecordingLogger<DonateCoinDomainService>();
        var service = CreateService(logger, coinDomainService);

        var exception = await Record.ExceptionAsync(() =>
            service.AddCoinsForVideos(CreateCookie())
        );

        Assert.Null(exception);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("跳过本次投币"));
    }

    private static DonateCoinDomainService CreateService(
        ILogger<DonateCoinDomainService> logger,
        ICoinDomainService coinDomainService
    ) =>
        new(
            logger,
            new StaticOptionsMonitor<DailyTaskOptions>(new()),
            null!,
            coinDomainService,
            null!,
            null!
        );

    private static BiliCookie CreateCookie() =>
        new(
            new Dictionary<string, string>
            {
                ["DedeUserID"] = "123",
                ["SESSDATA"] = "synthetic",
                ["bili_jct"] = "synthetic",
            }
        );

    private sealed class StubCoinDomainService : ICoinDomainService
    {
        public Exception? DonatedCoinsException { get; set; }

        public Exception? CoinBalanceException { get; set; }

        public int DonatedCoins { get; set; }

        public decimal CoinBalance { get; set; } = 10m;

        public Task<decimal> GetCoinBalance(BiliCookie ck) =>
            CoinBalanceException is null
                ? Task.FromResult(CoinBalance)
                : Task.FromException<decimal>(CoinBalanceException);

        public Task<int> GetDonatedCoins(BiliCookie ck) =>
            DonatedCoinsException is null
                ? Task.FromResult(DonatedCoins)
                : Task.FromException<int>(DonatedCoinsException);
    }

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
}
