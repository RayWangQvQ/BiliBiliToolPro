using System.Reflection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.AccountApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.DomainService;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

/// <summary>
/// 余额/投币经验接口返回 null 或业务错误时，CoinDomainService 必须抛出可被上层降级处理的
/// InvalidOperationException，而不是空引用崩溃。
/// </summary>
public sealed class CoinDomainServiceNullResponseTests
{
    [Fact]
    public async Task GetCoinBalance_NullResponse_ThrowsBusinessExceptionInsteadOfNullReference()
    {
        var service = new CoinDomainService(CreateAccountApi(null!), apiApi: null!);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCoinBalance(CreateCookie())
        );

        Assert.Contains("获取硬币余额失败", exception.Message);
    }

    [Fact]
    public async Task GetCoinBalance_ErrorCode_ThrowsBusinessException()
    {
        var service = new CoinDomainService(
            CreateAccountApi(new BiliApiResponse<CoinBalance> { Code = -1, Message = "账号异常" }),
            apiApi: null!
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCoinBalance(CreateCookie())
        );

        Assert.Contains("账号异常", exception.Message);
    }

    [Fact]
    public async Task GetCoinBalance_MissingData_ThrowsBusinessException()
    {
        var service = new CoinDomainService(
            CreateAccountApi(new BiliApiResponse<CoinBalance> { Code = 0 }),
            apiApi: null!
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCoinBalance(CreateCookie())
        );
    }

    [Fact]
    public async Task GetDonatedCoins_NullResponse_ThrowsBusinessExceptionInsteadOfNullReference()
    {
        var service = new CoinDomainService(null!, CreateApiApi(null!));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetDonatedCoins(CreateCookie())
        );

        Assert.Contains("获取投币经验失败", exception.Message);
    }

    [Fact]
    public async Task GetDonatedCoins_ErrorCode_ThrowsBusinessException()
    {
        var service = new CoinDomainService(
            null!,
            CreateApiApi(new BiliApiResponse<int> { Code = -101, Message = "未登录" })
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetDonatedCoins(CreateCookie())
        );

        Assert.Contains("未登录", exception.Message);
    }

    private static IAccountApi CreateAccountApi(BiliApiResponse<CoinBalance> response) =>
        Proxy.Create<IAccountApi>(
            (method, _) =>
                method == nameof(IAccountApi.GetCoinBalanceAsync)
                    ? Task.FromResult(response)
                    : throw new InvalidOperationException($"Unexpected API: {method}")
        );

    private static IApiApi CreateApiApi(BiliApiResponse<int> response) =>
        Proxy.Create<IApiApi>(
            (method, _) =>
                method == nameof(IApiApi.GetDonateCoinExpAsync)
                    ? Task.FromResult(response)
                    : throw new InvalidOperationException($"Unexpected API: {method}")
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
