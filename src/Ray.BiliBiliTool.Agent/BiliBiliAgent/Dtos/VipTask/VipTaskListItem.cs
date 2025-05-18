using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class VipTaskInfo
{
    public TaskInfo Task_info { get; set; }

    public void LogInfo(ILogger logger)
    {
        logger.LogInformation("------当前任务状态------");

        logger.LogInformation("打卡：{signed}", Task_info.Sing_task_item.IsTodaySigned ? "√" : "X");

        foreach (var moduleItem in Task_info.Modules)
        {
            logger.LogInformation("-{title}", moduleItem.module_title);
            foreach (var commonTaskItem in moduleItem.common_task_item)
            {
                logger.LogInformation(
                    "---{title}：{status}",
                    commonTaskItem.title,
                    commonTaskItem.state == 3 ? "√" : "X"
                );
            }
        }
        logger.LogInformation("------------------------" + Environment.NewLine);
    }
}

public class TaskInfo
{
    public int Score_month { get; set; }

    public int Score_limit { get; set; }

    public List<ModuleItem> Modules { get; set; }

    public SingTaskItem Sing_task_item { get; set; }
}

public class SingTaskItem
{
    public int Count { get; set; }

    public int Base_score { get; set; }

    public List<Histtory> Histories { get; set; } = new List<Histtory>();

    public Histtory TodayHistory => Histories.FirstOrDefault(x => x.Is_today);

    public bool IsTodaySigned => TodayHistory?.Signed == true;
}

public class ModuleItem
{
    public string module_title { get; set; }

    public List<CommonTaskItem> common_task_item { get; set; }
}

public class CommonTaskItem
{
    public string title { get; set; }

    public string subtitle { get; set; }

    public string explain { get; set; }

    public string task_code { get; set; }

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
