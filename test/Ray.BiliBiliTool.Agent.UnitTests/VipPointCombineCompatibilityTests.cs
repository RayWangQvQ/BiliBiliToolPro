using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Refit;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

public class VipPointCombineCompatibilityTests
{
    [Theory]
    [InlineData("omitted", false)]
    [InlineData("null", false)]
    [InlineData("legacy", false)]
    [InlineData("omitted", true)]
    [InlineData("null", true)]
    [InlineData("legacy", true)]
    public void Combine_PreservesTasksWithOptionalLegacySign(string sign, bool webOptions)
    {
        var response = JsonSerializer.Deserialize<BiliApiResponse<VipBigPointCombine>>(
            Payload(sign, webOptions),
            webOptions ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : null
        );
        AssertTasks(response);
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("legacy")]
    public async Task Refit_CombineAcceptsCurrentAndLegacyResponses(string sign)
    {
        using var client = new HttpClient(new ResponseHandler(Payload(sign, true)))
        {
            BaseAddress = new Uri("https://api.bilibili.com"),
        };
        var api = RestService.For<IApiApi>(client);
        var response = await api.GetCombineAsync(
            new GetCombineRequest { csrf = "synthetic-csrf", buvid = "synthetic-buvid" },
            "synthetic-cookie"
        );
        AssertTasks(response);
    }

    [Fact]
    public void LegacySign_PreservesHistoryAndSignedState()
    {
        var response = JsonSerializer.Deserialize<BiliApiResponse<VipBigPointCombine>>(
            Payload("legacy", false)
        );
#pragma warning disable CS0618
        var sign = Assert.IsType<SingTaskItem>(response!.Data!.Task_info.Sing_task_item);
#pragma warning restore CS0618
        Assert.Equal(3, sign.Count);
        Assert.Equal(10, sign.Base_score);
        Assert.True(sign.IsTodaySigned);
        Assert.Single(sign.Histories);
        Assert.Equal(10, sign.TodayHistory!.Score);
    }

    [Theory]
    [InlineData("Task_info")]
    [InlineData("point_info")]
    public void Combine_MissingRequiredTaskOrPointDataStillFails(string property)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(Payload("legacy", false))!;
        Assert.True(node["Data"]!.AsObject().Remove(property));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<BiliApiResponse<VipBigPointCombine>>(node.ToJsonString())
        );
    }

    private static void AssertTasks(BiliApiResponse<VipBigPointCombine>? response)
    {
        Assert.NotNull(response);
        Assert.Equal(0, response.Code);
        Assert.NotNull(response.Data);
        Assert.Equal(20, response.Data.point_info.point);
        var module = Assert.Single(response.Data.Task_info.Modules);
        Assert.Equal("daily", module.module_title);
        var task = Assert.Single(module.common_task_item);
        Assert.Equal("dress-view", task.task_code);
        Assert.Equal(3, task.state);
        Assert.Equal(1, task.complete_times);
    }

    private static string Payload(string sign, bool lowerCase)
    {
        var signField = sign switch
        {
            "omitted" => "",
            "null" => ",\"Sing_task_item\":null",
            "legacy" =>
                ",\"Sing_task_item\":{\"Count\":3,\"Base_score\":10,\"Histories\":[{\"Day\":\"2026-10-07T00:00:00\",\"Signed\":true,\"Score\":10,\"Is_today\":true}]}",
            _ => throw new ArgumentOutOfRangeException(nameof(sign)),
        };
        var payload =
            "{\"Code\":0,\"Data\":{\"point_info\":{\"point\":20,\"expire_point\":0,\"expire_time\":0,\"expire_days\":0},\"Task_info\":{\"Score_month\":20,\"Score_limit\":100,\"Modules\":[{\"module_title\":\"daily\",\"common_task_item\":[{\"title\":\"view\",\"task_code\":\"dress-view\",\"state\":3,\"complete_times\":1,\"max_times\":1}]}]"
            + signField
            + "}}}";
        if (lowerCase)
        {
            foreach (
                var property in new[]
                {
                    "Code",
                    "Data",
                    "Task_info",
                    "Score_month",
                    "Score_limit",
                    "Modules",
                    "Sing_task_item",
                    "Count",
                    "Base_score",
                    "Histories",
                    "Day",
                    "Signed",
                    "Score",
                    "Is_today",
                }
            )
            {
                payload = payload.Replace(
                    "\"" + property + "\"",
                    "\"" + property.ToLowerInvariant() + "\""
                );
            }
        }
        return payload;
    }

    private sealed class ResponseHandler(string payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Assert.Equal("/x/vip_point/task/combine", request.RequestUri!.AbsolutePath);
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                }
            );
        }
    }
}
