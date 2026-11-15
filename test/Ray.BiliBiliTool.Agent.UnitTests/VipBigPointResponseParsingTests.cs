using System.Text.Json;
using FluentAssertions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.VipBigPoint;
using Xunit;

namespace Ray.BiliBiliTool.Agent.UnitTests;

/// <summary>
/// The combine API does not always return the same shape: <c>sing_task_item</c> comes and goes.
/// Response parsing is what breaks first, so it is locked here.
/// </summary>
public class VipBigPointResponseParsingTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Some accounts/days get a <c>task_info</c> without <c>sing_task_item</c>. While the member was
    /// <c>required</c>, System.Text.Json threw "missing required properties" and the whole response,
    /// including the task list, was discarded.
    /// </summary>
    [Fact]
    public void Combine_WithoutSingTaskItem_KeepsTaskModules()
    {
        const string json = """
            {
              "code": 0,
              "message": "0",
              "ttl": 1,
              "data": {
                "point_info": { "point": 6880, "expire_point": 0, "expire_time": 0, "expire_days": 0 },
                "task_info": {
                  "score_month": 35,
                  "score_limit": 100,
                  "modules": [
                    {
                      "module_title": "日常任务",
                      "common_task_item": [
                        { "title": "浏览追番频道页10秒", "task_code": "animatetab", "state": 3 }
                      ]
                    }
                  ]
                }
              }
            }
            """;

        var response = JsonSerializer.Deserialize<BiliApiResponse<VipBigPointCombine>>(
            json,
            Options
        );

        response.Should().NotBeNull();
        response!.Data.Should().NotBeNull();
#pragma warning disable CS0618 // Sing_task_item is obsolete but still part of the payload contract
        response.Data!.Task_info.Sing_task_item.Should().BeNull();
#pragma warning restore CS0618
        response.Data.Task_info.Modules.Should().ContainSingle();
        response
            .Data.Task_info.Modules[0]
            .common_task_item.Should()
            .ContainSingle(x => x.task_code == "animatetab");
    }

    /// <summary>
    /// And the full payload (with <c>sing_task_item</c>) must keep parsing.
    /// </summary>
    [Fact]
    public void Combine_WithSingTaskItem_StillParses()
    {
        const string json = """
            {
              "code": 0,
              "data": {
                "point_info": { "point": 6880, "expire_point": 0, "expire_time": 0, "expire_days": 0 },
                "task_info": {
                  "score_month": 35,
                  "score_limit": 100,
                  "modules": [],
                  "sing_task_item": { "count": 1, "base_score": 10, "histories": [] }
                }
              }
            }
            """;

        var response = JsonSerializer.Deserialize<BiliApiResponse<VipBigPointCombine>>(
            json,
            Options
        );

        response!.Data!.Task_info.Modules.Should().BeEmpty();
    }
}
