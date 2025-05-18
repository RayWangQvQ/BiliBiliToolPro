using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Config.Options;

public class LiveLotteryTaskOptions : IHasCron
{
    public string IncludeAwardNames { get; set; }

    public string ExcludeAwardNames { get; set; }

    public List<string> IncludeAwardNameList =>
        IncludeAwardNames
            ?.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public List<string> ExcludeAwardNameList =>
        ExcludeAwardNames
            ?.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public bool AutoGroupFollowings { get; set; } = true;

    public string DenyUids { get; set; }

    public List<string> DenyUidList =>
        DenyUids
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public string Cron { get; set; }
}
