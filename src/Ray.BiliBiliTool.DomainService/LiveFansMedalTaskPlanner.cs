using System.Globalization;
using System.Text.RegularExpressions;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;

namespace Ray.BiliBiliTool.DomainService;

public record LiveFansMedalPlan(int Remaining, int RoundSize);

public static class LiveFansMedalTaskPlanner
{
    private static readonly Regex Number = new(
        @"\d+(?:\.\d+)?",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1)
    );
    private static readonly Regex Progress = new(
        @"(\d+(?:\.\d+)?)\s*/\s*(\d+(?:\.\d+)?)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1)
    );

    public static LiveFansMedalPlan Plan(ActivatedMedalResponse data, string action)
    {
        var task = data.Task_info.FirstOrDefault(item => item.Jump_type == action);
        if (task is null || task.Is_done || (data.Is_lighted && data.Reach_free_intimacy_limit))
            return new(0, 0);

        var quantity = Number.Match(task.Title);
        if (
            !quantity.Success
            || !decimal.TryParse(
                quantity.Value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var size
            )
            || size <= 0
            || size > 1440
        )
            throw new InvalidOperationException("无法读取粉丝牌任务要求，请查看 B 站任务页面");

        // Watching is measured in seconds. Other actions are measured in individual interactions.
        var multiplier = action == "watchLive" ? 60 : 1;
        var round = checked((int)decimal.Ceiling(size * multiplier));
        var progress = Progress.Match(task.Sub_title);
        if (!progress.Success)
        {
            if (!data.Is_lighted)
                return new(round, round);
            throw new InvalidOperationException("无法读取粉丝牌每日任务进度，请查看 B 站任务页面");
        }

        var current = decimal.Parse(progress.Groups[1].Value, CultureInfo.InvariantCulture);
        var limit = decimal.Parse(progress.Groups[2].Value, CultureInfo.InvariantCulture);
        if (limit > 1000)
            throw new InvalidOperationException("粉丝牌任务进度超出支持范围");
        return new(checked((int)decimal.Ceiling(Math.Max(0, limit - current) * round)), round);
    }
}
