using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class VipBigPointCombine
{
    public required PointInfo point_info { get; set; }
    public required TaskInfo Task_info { get; set; }

    public void LogFullInfo(ILogger logger)
    {
        logger.LogInformation("当前经验：{point}", point_info.point);
        // logger.LogInformation("打卡：{signed}", Task_info.Sing_task_item.IsTodaySigned ? "√" : "X");
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
    }

    public void LogPointInfo(ILogger logger)
    {
        logger.LogInformation("当前经验：{point}", point_info.point);
    }
}
