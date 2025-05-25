namespace Ray.BiliBiliTool.Config.Options;

public class UnfollowBatchedTaskOptions : IHasCron
{
    private const string DefaultGroupName = "天选时刻";

    public required string GroupName { get; set; } = DefaultGroupName;

    public int Count { get; set; }

    public string? RetainUids { get; set; }

    public List<string> RetainUidList =>
        RetainUids
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public string? Cron { get; set; }
}
