using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Refit;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class LiveMedalApiAndHeartbeatTests
{
    [Theory]
    [InlineData(0, "80070713463e7749b90c2dc24911e275")]
    [InlineData(1, "de7c9b85b8b78aa6bc8a7a36f70a90701c9db4d9")]
    [InlineData(2, "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8")]
    [InlineData(3, "88ff8b54675d39b8f72322e65ff945c52d96379988ada25639747e69")]
    [InlineData(
        4,
        "b42af09057bac1e2d41708e48a902e09b5ff7f12ab428a4fe86653c73dd248fb82f948a549f7b791a5b41915ee4d1ec3935357e4e2317250d0372afa2ebeeb3a"
    )]
    [InlineData(
        5,
        "d7f4727e2c0b39ae0f1e40cc96f60242d5b7801841cea6fc592c5d3e1ae50700582a96cf35e1e554995fe4e03381c237"
    )]
    public void HeartbeatSignature_AllServerAlgorithmsMatchIndependentVectors(
        int rule,
        string expected
    ) =>
        Assert.Equal(
            expected,
            LiveHeartBeatCrypto.Sypder("The quick brown fox jumps over the lazy dog", [rule], "key")
        );

    [Fact]
    public void HeartbeatSignature_Sha224HandlesLongKeysAndMultipleBlocks() =>
        Assert.Equal(
            "4cf8ac140e8e452bead2b89f608a60e185de98fb274b4590d67b1be1",
            LiveHeartBeatCrypto.Sypder(
                string.Concat(Enumerable.Repeat("payload", 100)),
                [3],
                new string('a', 100)
            )
        );

    [Fact]
    public async Task LiveMedalApis_RequestCurrentRoutesAndDeserializeProgressOffline()
    {
        using var handler = new Handler();
        using var client = new HttpClient(handler)
        {
            BaseAddress = new("https://api.live.bilibili.com"),
        };
        var api = RestService.For<ILiveApi>(client);
        var panel = await api.GetFansMedalPanel(2, "synthetic");
        Assert.Equal(60, panel.Data!.List[0].Medal.Level);
        Assert.Contains("page=2", handler.LastUri!.Query);
        Assert.Equal("/xlive/app-ucenter/v1/fansMedal/panel", handler.LastUri.AbsolutePath);
        var tasks = await api.GetActivatedMedalInfo(60, "synthetic", "synthetic");
        Assert.True(tasks.Data!.Is_lighted);
        Assert.Equal("like", tasks.Data.Task_info[0].Jump_type);
        Assert.Equal(
            "/xlive/app-ucenter/v1/fansMedal/GetActivatedMedalInfo",
            handler.LastUri.AbsolutePath
        );
        Assert.Contains("target_id=60", handler.LastUri.Query);
    }

    [Fact]
    public async Task HeartbeatRequests_KeepNormalizedFieldsAndReceiveWbiSignatures()
    {
        var signed = new List<Dictionary<string, string>>();
        var wbi = Proxy.Create<IWbiService>(
            (method, args) =>
            {
                Assert.Equal("GetWridAsync", method);
                signed.Add(new((Dictionary<string, string>)args[0]!));
                return Task.FromResult(new WridDto { wts = 123, w_rid = "offline-signature" });
            }
        );
        using var handler = new Handler();
        using var normalize = new FormUrlEncodedKeyNormalizingDelegatingHandler
        {
            InnerHandler = new WridEncryptionDelegatingHandler(wbi) { InnerHandler = handler },
        };
        using var client = new HttpClient(normalize)
        {
            BaseAddress = new("https://live-trace.bilibili.com"),
        };
        var api = RestService.For<ILiveTraceApi>(client);
        const string cookie = "DedeUserID=1; bili_jct=synthetic; SESSDATA=synthetic";
        await api.EnterRoom(
            new EnterRoomRequest(
                100,
                1,
                2,
                0,
                123000,
                "offline-test",
                "synthetic",
                60,
                "[\"buvid\",\"uuid\"]"
            ),
            cookie
        );
        var form = HttpUtility.ParseQueryString(handler.LastBody!);
        Assert.Equal("offline-signature", form["w_rid"]);
        Assert.Equal("60", form["ruid"]);
        Assert.Equal("444.8", form["web_location"]);
        await api.HeartBeat(
            new HeartBeatRequest(
                100,
                1,
                2,
                1,
                "buvid",
                153000,
                123,
                "offline-test",
                [2, 3, 4, 5],
                "synthetic",
                "synthetic",
                "uuid",
                "[\"buvid\",\"uuid\"]",
                30,
                60
            ),
            cookie
        );
        form = HttpUtility.ParseQueryString(handler.LastBody!);
        Assert.Equal("offline-signature", form["w_rid"]);
        Assert.Equal("30", form["time"]);
        Assert.Equal("60", form["ruid"]);
        Assert.Equal("-99998", form["trackid"]);
        Assert.Equal(2, signed.Count);
        Assert.All(
            signed,
            values =>
            {
                Assert.Contains("wts", values.Keys);
                Assert.Contains("id", values.Keys);
                Assert.DoesNotContain("Id", values.Keys);
            }
        );
    }

    [Fact]
    public void HeartbeatSignature_UnknownRulesAreReported() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LiveHeartBeatCrypto.Sypder("test", [6], "synthetic")
        );

    public class Proxy : DispatchProxy
    {
        public Func<string, object?[], object> Handler { get; set; } = null!;

        protected override object Invoke(MethodInfo? method, object?[]? args) =>
            Handler(method!.Name, args!);

        public static T Create<T>(Func<string, object?[], object> handler)
            where T : class
        {
            var value = Create<T, Proxy>();
            ((Proxy)(object)value).Handler = handler;
            return value;
        }
    }

    private class Handler : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken token
        )
        {
            LastUri = request.RequestUri;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(token);
            var json = LastUri!.AbsolutePath.EndsWith("panel")
                ? """{"code":0,"data":{"list":[{"medal":{"target_id":60,"level":60},"room_info":{"room_id":100}}],"special_list":[],"page_info":{"total_page":2}}}"""
                : """{"code":0,"data":{"is_lighted":true,"task_info":[{"title":"点赞30次","sub_title":"每日上限 0/10","jump_type":"like","is_done":false}]}}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }
    }
}
