namespace Ray.BiliBiliTool.Config.Options;

public class VipBigPointOptions : BaseConfigOptions
{
    public override string SectionName => "VipBigPointConfig";

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

    public override Dictionary<string, string> ToConfigDictionary()
    {
        return MergeConfigDictionary(
            new Dictionary<string, string>
            {
                { $"{SectionName}:{nameof(ViewBangumis)}", ViewBangumis ?? "" },
            }
        );
    }
}
