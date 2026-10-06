using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ray.BiliBiliTool.Web.UnitTests;

public class VipPrivilegeEnablementTests
{
    [Theory]
    [InlineData(true, false, TodayTaskItemState.Disabled)]
    [InlineData(false, true, TodayTaskItemState.NotDone)]
    [InlineData(false, false, TodayTaskItemState.Disabled)]
    [InlineData(true, true, TodayTaskItemState.NotDone)]
    public void DisabledPrivilege_UsesItsOwnSwitchAndCannotBeAutomaticallyRecovered(
        bool dailyEnabled,
        bool privilegeEnabled,
        TodayTaskItemState expected
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DailyTaskConfig:IsEnable"] = dailyEnabled.ToString(),
                    ["VipPrivilegeConfig:IsEnable"] = privilegeEnabled.ToString(),
                }
            )
            .Build();
        var task = TaskCatalog.All.Single(task => task.TaskKey == "VipPrivilegeTaskAppService");
        var item = Assert.Single(task.Items);
        var context = new TodayTaskItemContext
        {
            Task = task,
            Item = item,
            IsTaskEnabled = task.IsEnabled(configuration),
            IsItemEnabled = item.IsEnabled(configuration),
            HasFireTimeToday = true,
            IsPastDueTime = true,
            Records = [],
            AutoAttempts = 0,
            MaxAutoAttempts = 3,
        };
        var result = TaskStatusEvaluator.Evaluate(context);
        Assert.Equal(expected, result.State);
        Assert.Equal(
            expected != TodayTaskItemState.Disabled,
            TaskStatusEvaluator.CanAutoRedo(context, result)
        );
    }
}
