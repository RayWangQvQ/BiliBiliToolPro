namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class TaskInfo
{
    public int Score_month { get; set; }

    public int Score_limit { get; set; }

    public List<ModuleItem> Modules { get; set; } = [];

    [Obsolete(
        "The sign result comes from combine API is not correct, use IVipBigPointApi.GetThreeDaySignAsync instead."
    )]
    public required SingTaskItem Sing_task_item { get; set; }
}

public class SingTaskItem
{
    public int Count { get; set; }

    public int Base_score { get; set; }

    public List<Histtory> Histories { get; set; } = [];

    public Histtory? TodayHistory => Histories.FirstOrDefault(x => x.Is_today);

    public bool IsTodaySigned => TodayHistory?.Signed == true;
}

public class ModuleItem
{
    public required string module_title { get; set; }

    public List<CommonTaskItem> common_task_item { get; set; } = [];
}

public class CommonTaskItem
{
    public required string title { get; set; }

    public string? subtitle { get; set; }

    public string? explain { get; set; }

    public required string task_code { get; set; }

    public int state { get; set; }

    public int vip_limit { get; set; }

    public int complete_times { get; set; }

    public int max_times { get; set; }

    public int recall_num { get; set; }
}

public class Histtory
{
    public DateTime Day { get; set; }

    public bool Signed { get; set; }

    public int Score { get; set; }

    public bool Is_today { get; set; }
}
