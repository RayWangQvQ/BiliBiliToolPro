namespace Ray.BiliBiliTool.Config.Options;

public class VipBigPointOptions : IHasCron
{
    public const string SectionName = "VipBigPointConfig";

    public string? ViewBangumis { get; set; }

    public List<long> ViewBangumiList
    {
        get
        {
            List<long> re = [];
            if (string.IsNullOrWhiteSpace(ViewBangumis) | ViewBangumis == "-1")
                return re;

            string[] array = ViewBangumis?.Split(',') ?? [];
            foreach (string item in array)
            {
                if (long.TryParse(item.Trim(), out long upId))
                    re.Add(upId);
                else
                    re.Add(long.MinValue);
            }
            return re;
        }
    }

    public string? Cron { get; set; }

    public Dictionary<string, string> ToConfigDictionary()
    {
        var result = new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(ViewBangumis)}", ViewBangumis ?? "" },
            { $"{SectionName}:{nameof(Cron)}", Cron ?? "" },
        };
        return result;
    }
}
