using BlazingQuartz.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Config.SQLite;

namespace Ray.BiliBiliTool.Web.Components.Pages.Configs;

public abstract class BaseConfigComponent<T> : ComponentBase
    where T : BaseConfigOptions, new()
{
    [Inject]
    protected IConfiguration Configuration { get; set; } = null!;

    [Inject]
    protected ISchedulerService? SchedulerService { get; set; }

    [Inject]
    protected ISchedulerFactory? SchedulerFactory { get; set; }

    [Inject]
    protected ILogger<BaseConfigComponent<T>> Logger { get; set; } = null!;

    protected T _config = new();
    protected bool _isLoading = true;
    protected MarkupString? _saveMessage;
    protected bool _saveSuccess;

    protected abstract IOptionsMonitor<T> OptionsMonitor { get; }

    /// <summary>
    /// 获取对应的任务JobKey，如果返回null则不控制定时任务
    /// </summary>
    protected virtual JobKey? GetJobKey() => null;

    /// <summary>
    /// 获取触发器名称
    /// </summary>
    protected virtual string GetTriggerName(JobKey jobKey) => $"{jobKey}.Cron.Trigger";

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigAsync();
    }

    protected Task LoadConfigAsync()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            _config = OptionsMonitor.CurrentValue;
        }
        catch (Exception ex)
        {
            _saveMessage = new MarkupString($"Failed to load configuration: {ex.Message}");
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    protected virtual async Task HandleValidSubmitAsync()
    {
        _isLoading = true;
        _saveMessage = null;

        try
        {
            // 保存配置
            var sqliteProvider = GetSqliteConfigurationProvider();
            if (sqliteProvider == null)
            {
                throw new InvalidOperationException("Unable to get SqliteConfigurationProvider");
            }

            var configValues = _config.ToConfigDictionary();
            sqliteProvider.BatchSet(configValues);

            // 如果有对应的定时任务，同步更新 Quartz 任务状态和 Cron 表达式
            var jobKey = GetJobKey();
            if (jobKey != null && SchedulerService != null)
            {
                // 更新 Cron 表达式
                await UpdateJobCronAsync(jobKey, _config.Cron);

                // 控制任务启停
                await ControlScheduledJobAsyc(jobKey, _config.IsEnable);
            }

            _saveMessage = GetSaveSuccessMessage();
            _saveSuccess = true;
        }
        catch (Exception ex)
        {
            _saveMessage = new MarkupString($"Failed to save configuration: {ex.Message}");
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task ControlScheduledJobAsyc(JobKey jobKey, bool isEnable)
    {
        var triggerName = GetTriggerName(jobKey);
        var triggerGroup = Constants.BiliJobGroup;

        if (isEnable)
        {
            // 启用任务：恢复触发器
            await SchedulerService!.ResumeTrigger(triggerName, triggerGroup);
        }
        else
        {
            // 禁用任务：暂停触发器
            await SchedulerService!.PauseTrigger(triggerName, triggerGroup);
        }
    }

    private async Task UpdateJobCronAsync(JobKey jobKey, string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression) || SchedulerFactory == null)
            return;

        var triggerName = GetTriggerName(jobKey);
        var triggerKey = new TriggerKey(triggerName, Constants.BiliJobGroup);

        try
        {
            var scheduler = await SchedulerFactory.GetScheduler();

            // 创建新的 Cron 触发器
            var newTrigger = TriggerBuilder
                .Create()
                .WithIdentity(triggerKey)
                .ForJob(jobKey)
                .WithCronSchedule(cronExpression)
                .Build();

            // 重新调度触发器（替换现有的触发器）
            await scheduler.RescheduleJob(triggerKey, newTrigger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update cron expression for job {JobKey}", jobKey);
        }
    }

    private MarkupString GetSaveSuccessMessage()
    {
        var jobKey = GetJobKey();
        if (jobKey == null)
        {
            return new MarkupString("Configuration saved successfully!");
        }

        var status = _config.IsEnable ? "enabled" : "disabled";
        return new MarkupString(
            $"Configuration saved successfully!<br/>{jobKey} has been {status}."
        );
    }

    private SqliteConfigurationProvider? GetSqliteConfigurationProvider()
    {
        if (Configuration is IConfigurationRoot configRoot)
        {
            foreach (var provider in configRoot.Providers)
            {
                if (provider is SqliteConfigurationProvider sqliteProvider)
                {
                    return sqliteProvider;
                }
            }
        }
        return null;
    }
}
