using System.Text.Json;
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
    protected string? _saveMessage;
    protected bool _isSaving;
    private Dictionary<string, string> _savedValues = [];
    private bool _configLoaded;
    protected bool HasChanges =>
        _configLoaded
        && !_isLoading
        && !_savedValues
            .OrderBy(pair => pair.Key)
            .SequenceEqual(_config.ToConfigDictionary().OrderBy(pair => pair.Key));
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
        if (_isSaving)
            return Task.CompletedTask;
        _isLoading = true;
        _configLoaded = false;
        _saveMessage = null;
        _saveSuccess = false;

        try
        {
            // Edit an isolated draft so unsaved controls cannot affect scheduled jobs.
            _config = Clone(OptionsMonitor.CurrentValue);
            _savedValues = _config.ToConfigDictionary();
            _configLoaded = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load configuration");
            _saveMessage = "配置加载失败，请重新加载";
            _saveSuccess = false;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private static T Clone(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    protected virtual bool IsScheduleEnabled(T submitted) => submitted.IsEnable;

    protected virtual Dictionary<string, string> ValuesToPersist(T submitted) =>
        submitted.ToConfigDictionary();

    protected void UpdateSavedSetting(string key, string value) => _savedValues[key] = value;

    protected async Task<bool> SaveBeforeLeavingAsync()
    {
        await HandleValidSubmitAsync();
        return _saveSuccess && !HasChanges;
    }

    protected virtual async Task HandleValidSubmitAsync()
    {
        if (_isSaving || !HasChanges)
            return;
        _isSaving = true;
        _saveMessage = null;
        _saveSuccess = false;
        var persisted = false;
        // Capture exactly what this save submits, keeping subsequent edits separate from this snapshot.
        StateHasChanged();
        try
        {
            var originalCron = _config.Cron;
            var submitted = Clone(_config);
            await Task.Yield();
            if (string.IsNullOrWhiteSpace(submitted.Cron))
                submitted.Cron = Ray.BiliBiliTool.Web.Services.TaskSchedulePlan.DefaultCron;
            _ = new CronExpression(submitted.Cron);
            var provider =
                GetSqliteConfigurationProvider()
                ?? throw new InvalidOperationException(
                    "SQLite configuration provider is unavailable"
                );
            var values = ValuesToPersist(submitted);
            provider.BatchSet(values);
            persisted = true;
            ((IConfigurationRoot)Configuration).Reload();
            _savedValues = submitted.ToConfigDictionary();
            if (_config.Cron == originalCron)
                _config.Cron = submitted.Cron;
            var jobKey = GetJobKey();
            if (jobKey != null && SchedulerService != null)
            {
                await UpdateJobCronAsync(jobKey, submitted.Cron);
                await ControlScheduledJobAsyc(jobKey, IsScheduleEnabled(submitted));
            }
            _saveMessage = "配置已保存";
            _saveSuccess = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save configuration or update its schedule");
            _saveMessage = persisted
                ? "配置已保存，执行计划更新失败。请重新加载后检查任务状态。"
                : "保存失败，修改已保留，请重试";
        }
        finally
        {
            _isSaving = false;
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
            throw;
        }
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
