namespace Ray.BiliBiliTool.Config.Options;

public class LiveLotteryTaskOptions : BaseConfigOptions
{
    public override string SectionName => "LiveLotteryTaskConfig";

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

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(IncludeAwardNames)}", IncludeAwardNames ?? "" },
                { $"{SectionName}:{nameof(ExcludeAwardNames)}", ExcludeAwardNames ?? "" },
                {
                    $"{SectionName}:{nameof(AutoGroupFollowings)}",
                    AutoGroupFollowings.ToString().ToLower()
                },
                { $"{SectionName}:{nameof(DenyUids)}", DenyUids ?? "" },
            }
        );
    }
}
