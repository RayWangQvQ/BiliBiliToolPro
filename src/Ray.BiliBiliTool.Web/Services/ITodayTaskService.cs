using Ray.BiliBiliTool.Domain;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>页面上单个检查项的展示数据</summary>
public sealed class TodayTaskItemDto
{
    public string? ItemKey { get; init; }
    public required string DisplayName { get; init; }
    public required TodayTaskItemState State { get; init; }
    public required string StateText { get; init; }
    public string? Message { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int AutoAttempts { get; init; }

    /// <summary>今天是否已经跑过（用于区分「漏做」与「失败」）</summary>
    public bool AttemptedToday { get; init; }

    /// <summary>B 站状态还在查（首屏先用本地数据渲染，B 站状态随后异步补齐）</summary>
    public bool IsBiliPending { get; init; }

    /// <summary>是否显示「补做」按钮</summary>
    public bool CanRedo =>
        !IsBiliPending
        && State
            is TodayTaskItemState.NotDone
                or TodayTaskItemState.Failed
                or TodayTaskItemState.RetryExhausted;

    /// <summary>是否允许自动补做（分享恒为 false，见 TaskStatusEvaluator.CanAutoRedo）</summary>
    public bool CanAutoRedo { get; init; }

    /// <summary>是否显示「不再尝试」按钮（仅分享且仍开启时）</summary>
    public bool CanDisableShare { get; init; }
}

/// <summary>一个任务（页面上的一组）</summary>
public sealed class TodayTaskGroupDto
{
    public required string DisplayName { get; init; }
    public required string TaskKey { get; init; }
    public required List<TodayTaskItemDto> Items { get; init; }
}

/// <summary>一个账号今日的全部任务</summary>
public sealed class AccountTodayTasksDto
{
    public required long UserId { get; init; }
    public required string UserName { get; init; }
    public required int Index { get; init; }
    public required bool IsCookieValid { get; init; }
    public required List<TodayTaskGroupDto> Groups { get; init; }
}

public sealed record TaskRedoResultDto(bool Success, string Message);

public interface ITodayTaskService
{
    /// <summary>
    /// 取今日任务状态。
    /// <paramref name="includeBili"/> 为 false 时只读本地配置 + 数据库 + Quartz，不发任何网络请求，
    /// 用于首屏秒开；为 true 时再并行查 B 站把每日任务四项的真实状态补齐。
    /// </summary>
    Task<List<AccountTodayTasksDto>> GetTodayStatusAsync(
        bool includeBili,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 补做单项。trigger 用于区分「自动补做」与「手动补做」——
    /// 自动补做的次数上限就是靠统计 Trigger=Auto 的记录实现的，传错会导致上限失效。
    /// </summary>
    Task<TaskRedoResultDto> RedoAsync(
        long userId,
        string taskKey,
        string? itemKey,
        TaskRecordTrigger trigger = TaskRecordTrigger.Manual,
        CancellationToken cancellationToken = default
    );

    /// <summary>补做该账号所有可补做的项，返回执行条数</summary>
    Task<int> RedoAllForAccountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>补做全部账号所有可补做的项，返回执行条数</summary>
    Task<int> RedoAllMissingAsync(CancellationToken cancellationToken = default);

    /// <summary>一键关闭分享任务</summary>
    Task DisableShareAsync(CancellationToken cancellationToken = default);

    /// <summary>保存自动补做设置</summary>
    Task SaveAutoRecoverSettingsAsync(
        bool isEnable,
        int intervalHours,
        int retentionDays,
        CancellationToken cancellationToken = default
    );
}
