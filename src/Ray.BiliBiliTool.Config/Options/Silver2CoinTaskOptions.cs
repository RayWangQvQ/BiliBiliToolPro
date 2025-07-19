namespace Ray.BiliBiliTool.Config.Options;

public class Silver2CoinTaskOptions : IHasCron
{
    public const string SectionName = "Silver2CoinTaskConfig";

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
