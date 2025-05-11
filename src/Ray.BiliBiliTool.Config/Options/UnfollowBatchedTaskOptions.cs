using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Config.Options;

public class UnfollowBatchedTaskOptions : IHasCron
{
    public string GroupName { get; set; }

    public int Count { get; set; } = 0;

    public string RetainUids { get; set; }

    public List<string> RetainUidList =>
        RetainUids
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? new List<string>();

    public string Cron { get; set; }
}
