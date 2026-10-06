namespace Ray.BiliBiliTool.Web.Services;

public static class TaskFailureNotificationCatalog
{
    public static IReadOnlyList<(string TaskKey, string DisplayName)> All { get; } =
        TaskCatalog
            .All.Select(task => (task.TaskKey, task.DisplayName))
            .Append(("TestAppService", "测试任务"))
            .ToArray();

    public static string SettingKey(string taskKey) => $"TaskFailureNotification:Tasks:{taskKey}";

    public static string? TaskKeyForJob(string jobName) =>
        jobName switch
        {
            "TestBiliJob" => "TestAppService",
            _ => TaskCatalog.All.FirstOrDefault(task => task.JobName == jobName)?.TaskKey,
        };
}
