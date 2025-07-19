namespace Ray.BiliBiliTool.Config.Options;

public class LiveLotteryTaskOptions : IHasCron
{
    public const string SectionName = "LiveLotteryTaskConfig";

    public string? IncludeAwardNames { get; set; }

    public string? ExcludeAwardNames { get; set; }

    public List<string> IncludeAwardNameList =>
        IncludeAwardNames
            ?.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public List<string> ExcludeAwardNameList =>
        ExcludeAwardNames
            ?.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public bool AutoGroupFollowings { get; set; } = true;

    public string? DenyUids { get; set; }

    public List<string> DenyUidList =>
        DenyUids
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public string? Cron { get; set; }

    public Dictionary<string, string> ToConfigDictionary()
    {
        var result = new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(IncludeAwardNames)}", IncludeAwardNames ?? "" },
            { $"{SectionName}:{nameof(ExcludeAwardNames)}", ExcludeAwardNames ?? "" },
            {
                $"{SectionName}:{nameof(AutoGroupFollowings)}",
                AutoGroupFollowings.ToString().ToLower()
            },
            { $"{SectionName}:{nameof(DenyUids)}", DenyUids ?? "" },
            { $"{SectionName}:{nameof(Cron)}", Cron ?? "" },
        };
        return result;
    }
}
