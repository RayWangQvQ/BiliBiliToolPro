using System.Net;
using System.Text;
using FluentAssertions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Coin;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Refit;

namespace Ray.BiliBiliTool.Agent.FunctionalTests;

/// <summary>
/// 迁移到 Refit 后，参数名变成了 CLR 属性名原样（?Vmid=...、Aid=...），
/// 而 B 站的参数名区分大小写，大小写不符时直接返回 {"code":-400,"message":"请求错误"}。
/// 这里锁定实际发出的报文 key，防止再次退化。
/// </summary>
public class RequestParameterKeyTests
{
    [Fact]
    public async Task GetFollowings_ShouldSendLowerFirstCharQueryKeys()
    {
        var captured = new CapturingHandler();
        using var client = new HttpClient(captured)
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        var api = RestService.For<IApiApi>(
            client,
            new RefitSettings { UrlParameterKeyFormatter = new BiliUrlParameterKeyFormatter() }
        );

        _ = await api.GetFollowings(new GetFollowingsRequest(1585227649), "cookie");

        captured
            .RequestUri.Should()
            .Be(
                "https://api.bilibili.com/x/relation/followings"
                    + "?vmid=1585227649&order_type=attention&pn=1&ps=20&order=desc&jsonp=jsonp"
            );
    }

    [Fact]
    public async Task AddCoinForVideo_ShouldSendLowerFirstCharFormKeys()
    {
        var captured = new CapturingHandler();
        using var normalizing = new FormUrlEncodedKeyNormalizingDelegatingHandler
        {
            InnerHandler = captured,
        };
        using var client = new HttpClient(normalizing)
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        var api = RestService.For<IApiApi>(client);

        _ = await api.AddCoinForVideo(new AddCoinRequest(1776, "some_jct"), "cookie");

        captured
            .Body.Should()
            .Be(
                "aid=1776&multiply=1&select_like=1&cross_domain=true"
                    + "&csrf=some_jct&eab_x=2&ramval=3&source=web_normal&ga=1"
            );
        captured.ContentType.Should().Be("application/x-www-form-urlencoded");
    }

    [Fact]
    public async Task AddCoinForVideo_WithoutNormalizingHandlerSendsPascalCaseKeys()
    {
        var captured = new CapturingHandler();
        using var client = new HttpClient(captured)
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        var api = RestService.For<IApiApi>(client);

        _ = await api.AddCoinForVideo(new AddCoinRequest(1776, "some_jct"), "cookie");

        captured.Body.Should().Contain("Aid=1776").And.Contain("Csrf=some_jct");
    }

    /// <summary>
    /// B 站的错误信封没有 data 字段，Data 必须可空，
    /// 否则解析阶段就抛异常，业务错误码会被掩盖成 "An error occured deserializing the response."。
    /// </summary>
    [Fact]
    public async Task ErrorEnvelopeWithoutData_KeepsBusinessCode()
    {
        using var handler = new CapturingHandler
        {
            ResponseBody = """{"code":-400,"message":"请求错误","ttl":1}""",
        };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        var api = RestService.For<IApiApi>(
            client,
            new RefitSettings { UrlParameterKeyFormatter = new BiliUrlParameterKeyFormatter() }
        );

        var re = await api.GetFollowings(new GetFollowingsRequest(1585227649), "cookie");

        re.Code.Should().Be(-400);
        re.Message.Should().Be("请求错误");
        re.Data.Should().BeNull();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string ResponseBody { get; init; } =
            "{\"code\":0,\"message\":\"0\",\"ttl\":1,\"data\":{}}";

        public string? RequestUri { get; private set; }
        public string? Body { get; private set; }
        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestUri = request.RequestUri?.ToString();
            ContentType = request.Content?.Headers.ContentType?.ToString();
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
