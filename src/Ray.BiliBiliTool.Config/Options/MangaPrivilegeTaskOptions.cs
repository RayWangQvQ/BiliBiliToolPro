namespace Ray.BiliBiliTool.Config.Options;

public class MangaPrivilegeTaskOptions : IHasCron
{
    public const string SectionName = "MangaPrivilegeTaskConfig";

    public string? Cron { get; set; }

    public Dictionary<string, string> ToConfigDictionary()
    {
        var result = new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(Cron)}", Cron ?? "" },
        };
        return result;
    }
}
