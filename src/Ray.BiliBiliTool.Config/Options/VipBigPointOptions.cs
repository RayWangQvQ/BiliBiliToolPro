using System.Collections.Generic;

namespace Ray.BiliBiliTool.Config.Options;

public class VipBigPointOptions : IHasCron
{
    public string ViewBangumis { get; set; }

    public List<long> ViewBangumiList
    {
        get
        {
            List<long> re = new();
            if (string.IsNullOrWhiteSpace(ViewBangumis) | ViewBangumis == "-1")
                return re;

            string[] array = ViewBangumis.Split(',');
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

    public string Cron { get; set; }
}
