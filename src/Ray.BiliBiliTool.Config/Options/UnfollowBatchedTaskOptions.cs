namespace Ray.BiliBiliTool.Config.Options;

public class UnfollowBatchedTaskOptions : BaseConfigOptions
{
    public override string SectionName => "UnfollowBatchedTaskConfig";
    private const string DefaultGroupName = "天选时刻";

    public string GroupName { get; set; } = DefaultGroupName;

    public int Count { get; set; }

    public string? RetainUids { get; set; }

    public List<string> RetainUidList =>
        RetainUids
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(GroupName)}", GroupName },
                { $"{SectionName}:{nameof(Count)}", Count.ToString() },
                { $"{SectionName}:{nameof(RetainUids)}", RetainUids ?? "" },
            }
        );
    }
}
