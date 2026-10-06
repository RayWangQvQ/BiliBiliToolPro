using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain;
using Ray.BiliBiliTool.Domain.Exceptions;

namespace Ray.BiliBiliTool.DomainService;

public class LiveFansMedalTaskRunner(
    ILiveApi liveApi,
    ILiveTraceApi traceApi,
    ILogger logger,
    LiveFansMedalTaskOptions options,
    string userAgent,
    Func<TimeSpan, CancellationToken, Task>? delay = null,
    LiveFansMedalExecutionGate? executionGate = null,
    ILiveFansMedalProgressObserver? progressObserver = null,
    TimeProvider? clock = null,
    LiveWatchDiagnostics? watchDiagnostics = null
)
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private int _likesSent;
    private readonly LiveFansMedalExecutionGate _executionGate = executionGate ?? new(clock);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private bool FollowDaily => !options.UseLiveStateMonitoring && options.FollowDailyTaskLimit;

    public Task RunForAnchorAsync(
        BiliCookie cookie,
        long anchorId,
        long roomId,
        string action,
        CancellationToken token = default
    )
    {
        var medal = new FansMedalPanelItem
        {
            Medal = new() { Target_id = anchorId, Medal_name = $"主播 {anchorId}" },
            Room_info = new() { Room_id = roomId },
        };
        var included = options.GetIncludedAnchorIds();
        var eligible =
            anchorId > 0
            && roomId > 0
            && !options.GetExcludedAnchorIds().Contains(anchorId)
            && (!options.OnlySelectedAnchors || included.Contains(anchorId));
        return RunMedalsAsync(
            cookie,
            action,
            () => Task.FromResult(eligible ? new List<FansMedalPanelItem> { medal } : []),
            token
        );
    }

    public Task RunAsync(BiliCookie cookie, string action, CancellationToken token = default) =>
        RunMedalsAsync(cookie, action, () => GetMedalsAsync(cookie, token), token);

    private async Task RunMedalsAsync(
        BiliCookie cookie,
        string action,
        Func<Task<List<FansMedalPanelItem>>> readMedals,
        CancellationToken token
    )
    {
        var enabled = action switch
        {
            "like" => options.EnableLike,
            "sendDanmu" => options.EnableDanmaku,
            "watchLive" => options.EnableWatch,
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
        var customBudget = options.GetInteractionLimit(action);
        if (!enabled || (!FollowDaily && customBudget == 0))
        {
            TaskRecoveryProgressScope.Report(
                $"action/{action}",
                ActionName(action),
                TaskRecoveryProgressState.Skipped,
                !enabled ? "该动作已关闭" : "设置的执行额度为 0"
            );
            return;
        }

        TaskRecoveryProgressScope.Report(
            $"action/{action}",
            ActionName(action),
            TaskRecoveryProgressState.Running,
            "正在读取粉丝牌列表"
        );
        var medals = await readMedals();
        TaskRecoveryProgressScope.Report(
            $"action/{action}",
            ActionName(action),
            TaskRecoveryProgressState.Running,
            $"本轮检查 {medals.Count} 个粉丝牌"
        );
        var failures = 0;
        async Task ExecuteAsync(FansMedalPanelItem medal)
        {
            using var lease = _executionGate.TryAcquire(cookie, medal.Medal.Target_id, action);
            if (lease is null)
            {
                ReportMedal(
                    cookie,
                    medal,
                    action,
                    TaskRecoveryProgressState.Pending,
                    "后台正在执行同一动作，本次不重复发送"
                );
                return;
            }
            try
            {
                ReportMedal(
                    cookie,
                    medal,
                    action,
                    TaskRecoveryProgressState.Running,
                    "正在获取今日任务要求和进度"
                );
                token.ThrowIfCancellationRequested();
                var data = await GetTasksAsync(cookie, medal, token);
                if (action == "watchLive" && !data.Is_lighted)
                {
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        TaskRecoveryProgressState.Pending,
                        "等待粉丝牌点亮后开始观看",
                        data: data
                    );
                    return;
                }
                var plan = LiveFansMedalTaskPlanner.Plan(data, action);
                if (
                    options.UseLiveStateMonitoring
                    && plan.Remaining > 0
                    && _executionGate.Progress(cookie.UserId, medal.Medal.Target_id, action).Pending
                        > 0
                )
                {
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        TaskRecoveryProgressState.Pending,
                        ConfiguredLimitMessage(action, cookie, medal),
                        data: data
                    );
                    return;
                }
                var available = options.UseLiveStateMonitoring
                    ? _executionGate.Remaining(
                        cookie.UserId,
                        medal.Medal.Target_id,
                        action,
                        customBudget,
                        plan.Completed
                    )
                    : customBudget;
                var target = Math.Min(
                    plan.Remaining,
                    FollowDaily
                        ? action == "like"
                            ? 5000
                            : action == "sendDanmu"
                                ? 100
                                : 86400
                        : available
                );
                if (action == "like")
                    target = Math.Min(target, 5000 - _likesSent);
                if (target <= 0)
                {
                    var missing = !data.Task_info.Any(task => task.Jump_type == action);
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        missing ? TaskRecoveryProgressState.Skipped
                            : plan.Remaining == 0 ? TaskRecoveryProgressState.Completed
                            : TaskRecoveryProgressState.Pending,
                        missing ? "B 站今日没有该动作任务"
                            : plan.Remaining == 0
                                ? data.Is_lighted && data.Reach_free_intimacy_limit
                                        ? "B 站今日亲密度已达上限，本次不再执行"
                                    : "B 站已确认今日任务完成，本次不再执行"
                            : action == "like" && _likesSent >= 5000
                                ? "工具的本轮账号点赞保护额度（5000 次）已用完"
                            : ConfiguredLimitMessage(action, cookie, medal),
                        data: data
                    );
                    return;
                }

                ReportMedal(
                    cookie,
                    medal,
                    action,
                    TaskRecoveryProgressState.Waiting,
                    action == "watchLive" ? "等待空闲观看位置" : "准备发送互动",
                    0,
                    target,
                    data
                );
                if (action == "watchLive")
                {
                    using (await _executionGate.AcquireWatchSlotAsync(cookie.UserId, token))
                    {
                        // Progress may change while this watch session waits for a slot.
                        data = await GetTasksAsync(cookie, medal, token);
                        plan = LiveFansMedalTaskPlanner.Plan(data, action);
                        if (plan.Remaining == 0)
                        {
                            ReportMedal(
                                cookie,
                                medal,
                                action,
                                TaskRecoveryProgressState.Completed,
                                "B 站已确认今日任务完成，本次不再执行",
                                data: data
                            );
                            return;
                        }
                        if (!data.Is_lighted)
                        {
                            ReportMedal(
                                cookie,
                                medal,
                                action,
                                TaskRecoveryProgressState.Pending,
                                "等待粉丝牌点亮后开始观看",
                                data: data
                            );
                            return;
                        }
                        target = Math.Min(target, plan.Remaining);
                        if (options.UseLiveStateMonitoring)
                            target = Math.Min(
                                target,
                                _executionGate.Remaining(
                                    cookie.UserId,
                                    medal.Medal.Target_id,
                                    action,
                                    customBudget,
                                    plan.Completed
                                )
                            );
                        if (target <= 0)
                        {
                            ReportMedal(
                                cookie,
                                medal,
                                action,
                                TaskRecoveryProgressState.Pending,
                                ConfiguredLimitMessage(action, cookie, medal),
                                data: data
                            );
                            return;
                        }
                        await WatchAsync(cookie, medal, target, data, token);
                    }
                }
                else
                {
                    await InteractAsync(cookie, medal, data, action, target, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                ReportMedal(
                    cookie,
                    medal,
                    action,
                    TaskRecoveryProgressState.Failed,
                    TaskRecoveryProgressScope.DescribeFailure(ex)
                );
                Interlocked.Increment(ref failures);
                logger.LogError(
                    "粉丝牌 {medal} 的 {action} 任务失败：{reason}",
                    medal.Medal.Medal_name,
                    action,
                    ex is BiliBusinessException or InvalidOperationException
                        ? ex.Message
                        : ex.GetType().Name
                );
            }
        }

        if (action == "watchLive")
            await Task.WhenAll(medals.Select(ExecuteAsync));
        else
            foreach (var medal in medals)
                await ExecuteAsync(medal);

        if (failures > 0)
            throw new BiliBusinessException($"{failures} 个粉丝牌的 {action} 任务执行失败");
        TaskRecoveryProgressScope.Report(
            $"action/{action}",
            ActionName(action),
            TaskRecoveryProgressState.Completed,
            $"已检查 {medals.Count} 个粉丝牌，结果见各主播记录"
        );
    }

    private async Task<List<FansMedalPanelItem>> GetMedalsAsync(
        BiliCookie cookie,
        CancellationToken token
    )
    {
        var result = new Dictionary<long, FansMedalPanelItem>();
        var excluded = options.GetExcludedAnchorIds();
        var included = options.GetIncludedAnchorIds();
        for (var page = 1; page <= 1000; page++)
        {
            var data = Require(
                await liveApi.GetFansMedalPanel(page, cookie.ToString(), token),
                "获取粉丝牌列表"
            );
            foreach (var item in data.Special_list.Concat(data.List))
            {
                if (
                    item.Medal.Target_id > 0
                    && item.Room_info.Room_id > 0
                    && !excluded.Contains(item.Medal.Target_id)
                    && (!options.OnlySelectedAnchors || included.Contains(item.Medal.Target_id))
                )
                    result.TryAdd(item.Medal.Target_id, item);
            }
            if (!data.Page_info.Has_more && page >= data.Page_info.Total_page)
                return result.Values.ToList();
            await _delay(TimeSpan.FromMilliseconds(500), token);
        }
        throw new BiliBusinessException("粉丝牌列表分页未完成");
    }

    private async Task<ActivatedMedalResponse> GetTasksAsync(
        BiliCookie cookie,
        FansMedalPanelItem medal,
        CancellationToken token
    )
    {
        var observedAt = DateTimeOffset.UtcNow;
        var progress = Require(
            await liveApi.GetActivatedMedalInfo(
                medal.Medal.Target_id,
                cookie.BiliJct,
                cookie.ToString(),
                token
            ),
            "获取粉丝牌任务进度"
        );
        if (options.UseLiveStateMonitoring)
            foreach (var action in new[] { "like", "sendDanmu", "watchLive" })
            {
                // Unknown labels for unrelated actions must not block a valid action.
                LiveFansMedalPlan plan;
                try
                {
                    plan = LiveFansMedalTaskPlanner.Plan(progress, action);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
                _executionGate.Confirm(
                    cookie.UserId,
                    medal.Medal.Target_id,
                    action,
                    plan.Completed,
                    progress.Task_info.Any(task => task.Jump_type == action && task.Is_done)
                );
            }
        try
        {
            progressObserver?.Report(cookie, medal.Medal.Target_id, progress, observedAt);
        }
        catch (Exception error)
        {
            logger.LogDebug("Medal progress publication failed: {ErrorType}", error.GetType().Name);
        }
        return progress;
    }

    private async Task<int> InteractAsync(
        BiliCookie cookie,
        FansMedalPanelItem medal,
        ActivatedMedalResponse data,
        string action,
        int target,
        CancellationToken token
    )
    {
        var sent = 0;
        var failures = 0;
        string? lastRejection = null;
        void Stop(string reason)
        {
            ReportMedal(
                cookie,
                medal,
                action,
                failures > 0 ? TaskRecoveryProgressState.Failed : TaskRecoveryProgressState.Pending,
                lastRejection is null ? reason : $"{lastRejection}。{reason}",
                sent,
                target,
                data
            );
            if (lastRejection is not null)
                throw new BiliBusinessException($"{lastRejection}。{reason}");
        }
        var previous = LiveFansMedalTaskPlanner.Plan(data, action);
        while (sent < target && previous.Remaining > 0)
        {
            var roundTarget = Math.Min(
                target - sent,
                Math.Min(previous.Remaining, previous.RoundSize)
            );
            var roundSent = 0;
            while (roundSent < roundTarget)
            {
                token.ThrowIfCancellationRequested();
                var room = Require(
                    await liveApi.GetLiveRoomInfo(medal.Room_info.Room_id),
                    "获取直播状态"
                );
                if (action == "like" && room.Live_Status != 1)
                {
                    Stop("主播未开播，等待开播后点赞");
                    return sent;
                }
                if (
                    action == "sendDanmu"
                    && options.DanmakuOnlyWhenOffline
                    && room.Live_Status == 1
                )
                {
                    Stop("已设置仅未开播时发弹幕，等待主播下播");
                    return sent;
                }
                // A live room can be lit with likes without additional chat messages.
                if (
                    action == "sendDanmu"
                    && !data.Is_lighted
                    && room.Live_Status == 1
                    && options.EnableLike
                )
                {
                    Stop("将通过直播点赞点亮粉丝牌");
                    return sent;
                }
                var amount = action == "like" ? Math.Min(10, roundTarget - roundSent) : 1;
                if (action == "like")
                {
                    amount = Math.Min(amount, 5000 - _likesSent);
                    if (amount <= 0)
                    {
                        Stop("工具的本轮账号点赞保护额度（5000 次）已用完");
                        return sent;
                    }
                }
                var reservationDate = default(DateTime);
                if (options.UseLiveStateMonitoring)
                {
                    amount = _executionGate.ReserveInteraction(
                        cookie,
                        medal.Medal.Target_id,
                        action,
                        options.GetInteractionLimit(action),
                        amount,
                        out reservationDate
                    );
                    if (amount <= 0)
                    {
                        Stop(
                            action == "like" && _executionGate.RemainingMonitoredLikes(cookie) == 0
                                ? "工具的账号每日点赞保护额度（5000 次）已用完"
                                : ConfiguredLimitMessage(action, cookie, medal)
                        );
                        return sent;
                    }
                }
                BiliApiResponse response;
                if (action == "like")
                    response = await liveApi.LikeLiveRoom(
                        new LikeLiveRoomRequest(
                            medal.Room_info.Room_id,
                            cookie.BiliJct,
                            amount,
                            medal.Medal.Target_id,
                            cookie.UserId
                        ).RawTextBuild(),
                        cookie.ToString()
                    );
                else
                    response = await liveApi.SendLiveDanmuku(
                        new SendLiveDanmukuRequest(
                            cookie.BiliJct,
                            medal.Room_info.Room_id,
                            options.DanmakuContent
                        )
                        {
                            Rnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        },
                        cookie.ToString()
                    );
                if (response.Code != 0)
                {
                    if (options.UseLiveStateMonitoring)
                        _executionGate.ReleaseRejectedInteraction(
                            cookie,
                            medal.Medal.Target_id,
                            action,
                            amount,
                            reservationDate
                        );
                    lastRejection =
                        $"B 站未接受本次互动，错误码 {response.Code}：{response.Message}";
                    failures++;
                    var threshold =
                        action == "like"
                            ? 3
                            : Math.Clamp(options.SendDanmakugiveUpThreshold, 1, 10);
                    if (failures >= threshold)
                        throw new BiliBusinessException(
                            $"{lastRejection}。连续 {failures} 次失败后停止"
                        );
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        TaskRecoveryProgressState.Running,
                        $"{lastRejection}。正在重试 {failures}/{threshold}",
                        sent,
                        target,
                        data
                    );
                }
                else
                {
                    sent += amount;
                    roundSent += amount;
                    failures = 0;
                    if (action == "like")
                        _likesSent += amount;
                    lastRejection = null;
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        TaskRecoveryProgressState.Running,
                        "互动已发送，等待 B 站确认进度",
                        sent,
                        target,
                        data
                    );
                }
                await _delay(TimeSpan.FromSeconds(action == "like" ? 3 : 7), token);
            }

            var wasLighted = data.Is_lighted;
            data = await GetTasksAsync(cookie, medal, token);
            var latest = LiveFansMedalTaskPlanner.Plan(data, action);
            LogProgress(medal, data, action);
            ReportMedal(
                cookie,
                medal,
                action,
                TaskRecoveryProgressState.Running,
                "已更新 B 站任务进度",
                sent,
                target,
                data
            );
            // A lighting task changes into a different daily task after lighting.
            if (!wasLighted && data.Is_lighted)
            {
                if (!FollowDaily && !options.UseLiveStateMonitoring)
                {
                    ReportMedal(
                        cookie,
                        medal,
                        action,
                        TaskRecoveryProgressState.Pending,
                        "粉丝牌已点亮，本轮设置的互动已结束",
                        sent,
                        target,
                        data
                    );
                    return sent;
                }
                target = options.UseLiveStateMonitoring
                    ? Math.Min(target, sent + latest.Remaining)
                    : Math.Min(action == "like" ? 5000 : 100, sent + latest.Remaining);
                previous = latest;
                continue;
            }
            if (roundSent >= previous.RoundSize && latest.Remaining >= previous.Remaining)
            {
                await _delay(TimeSpan.FromSeconds(3), token);
                data = await GetTasksAsync(cookie, medal, token);
                latest = LiveFansMedalTaskPlanner.Plan(data, action);
                if (latest.Remaining >= previous.Remaining)
                {
                    if (options.UseLiveStateMonitoring)
                    {
                        ReportMedal(
                            cookie,
                            medal,
                            action,
                            TaskRecoveryProgressState.Pending,
                            ConfiguredLimitMessage(action, cookie, medal),
                            sent,
                            target,
                            data
                        );
                        return sent;
                    }
                    throw new BiliBusinessException("互动已发送，B 站任务进度尚未更新");
                }
            }
            previous = latest;
        }
        ReportMedal(
            cookie,
            medal,
            action,
            previous.Remaining == 0
                ? TaskRecoveryProgressState.Completed
                : TaskRecoveryProgressState.Pending,
            previous.Remaining == 0
                ? "平台已确认该任务完成"
                : "本轮互动已结束，平台任务仍有剩余额度",
            sent,
            target,
            data
        );
        return sent;
    }

    private async Task WatchAsync(
        BiliCookie cookie,
        FansMedalPanelItem medal,
        int target,
        ActivatedMedalResponse initialProgress,
        CancellationToken token
    )
    {
        using var diagnostic = watchDiagnostics?.Begin(
            cookie.UserId,
            medal.Medal.Target_id,
            _executionGate.WatchConcurrency
        );
        try
        {
            var roomId = medal.Room_info.Room_id;
            var room = Require(await liveApi.GetLiveRoomInfo(roomId), "获取直播间分区");
            diagnostic?.Start(
                room.Live_Status == 1,
                LiveFansMedalTaskPlanner.Plan(initialProgress, "watchLive").Completed,
                initialProgress
                    .Task_info.FirstOrDefault(task => task.Jump_type == "watchLive")
                    ?.Is_done == true,
                initialProgress.Reach_free_intimacy_limit
            );
            if (room.Parent_area_id <= 0 || room.Area_id <= 0)
            {
                ReportMedal(
                    cookie,
                    medal,
                    "watchLive",
                    TaskRecoveryProgressState.Pending,
                    "直播间尚未提供有效分区，暂时无法建立观看会话"
                );
                return;
            }
            var uuid = Guid.NewGuid().ToString();
            var device = $"[\"{cookie.LiveBuvid}\",\"{uuid}\"]";
            async Task<HeartBeatResponse> EnterAsync()
            {
                var requestStarted = _clock.GetUtcNow();
                var response = await traceApi.EnterRoom(
                    new EnterRoomRequest(
                        roomId,
                        room.Parent_area_id,
                        room.Area_id,
                        0,
                        _clock.GetUtcNow().ToUnixTimeMilliseconds(),
                        userAgent,
                        cookie.BiliJct,
                        medal.Medal.Target_id,
                        device
                    ),
                    cookie.ToString()
                );
                diagnostic?.Entry(
                    response.Code,
                    (long)(_clock.GetUtcNow() - requestStarted).TotalMilliseconds
                );
                return Require(response, "进入直播间");
            }
            var state = await EnterAsync();
            var watched = 0;
            var sequence = 1;
            var failures = 0;
            var lastCheck = 0;
            ActivatedMedalResponse? confirmed = null;
            while (watched < target)
            {
                if (
                    state.Heartbeat_interval <= 0
                    || state.Heartbeat_interval > 300
                    || string.IsNullOrEmpty(state.Secret_key)
                    || state.Timestamp <= 0
                    || state.Timestamp > DateTimeOffset.MaxValue.ToUnixTimeSeconds() - 300
                )
                    throw new BiliBusinessException("直播观看任务返回了无效的心跳参数");
                var interval = state.Heartbeat_interval;
                if (options.UseLiveStateMonitoring && interval > target - watched)
                {
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Pending,
                        "本轮剩余时长不足一个心跳间隔，已停止发送",
                        watched,
                        target,
                        confirmed
                    );
                    return;
                }
                // Use the server timeline so API and progress-read latency cannot accumulate.
                var due = DateTimeOffset.FromUnixTimeSeconds(state.Timestamp).AddSeconds(interval);
                var wait = due - _clock.GetUtcNow();
                if (wait > TimeSpan.FromSeconds(interval + 5))
                    throw new BiliBusinessException("直播观看心跳时间参数与当前时间不一致");
                if (wait > TimeSpan.Zero)
                {
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Waiting,
                        $"观看会话保持中，约 {Math.Ceiling(wait.TotalSeconds)} 秒后发送下次心跳",
                        watched,
                        target,
                        confirmed
                    );
                    await _delay(wait, token);
                }
                token.ThrowIfCancellationRequested();
                if (_clock.GetUtcNow() - due > TimeSpan.FromSeconds(5))
                {
                    if (++failures >= Math.Clamp(options.HeartBeatSendGiveUpThreshold, 1, 10))
                        throw new BiliBusinessException("直播观看心跳发送持续延迟，稍后重试");
                    logger.LogWarning("直播观看心跳发送延迟，正在重新建立观看会话");
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Running,
                        $"心跳发送延迟，正在重建会话 {failures}/{options.HeartBeatSendGiveUpThreshold}",
                        watched,
                        target,
                        confirmed
                    );
                    state = await EnterAsync();
                    sequence = 1;
                    continue;
                }
                // Keep the room and area identity from entry for the entire heartbeat session.
                var reservationDate = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(8)).Date;
                if (
                    options.UseLiveStateMonitoring
                    && _executionGate.Reserve(
                        cookie.UserId,
                        medal.Medal.Target_id,
                        "watchLive",
                        options.GetInteractionLimit("watchLive"),
                        interval,
                        whole: true
                    ) == 0
                )
                {
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Pending,
                        ConfiguredLimitMessage("watchLive", cookie, medal),
                        watched,
                        target,
                        confirmed
                    );
                    return;
                }
                var heartbeatStarted = _clock.GetUtcNow();
                var response = await traceApi.HeartBeat(
                    new HeartBeatRequest(
                        roomId,
                        room.Parent_area_id,
                        room.Area_id,
                        sequence,
                        cookie.LiveBuvid,
                        _clock.GetUtcNow().ToUnixTimeMilliseconds(),
                        state.Timestamp,
                        userAgent,
                        state.Secret_rule,
                        state.Secret_key,
                        cookie.BiliJct,
                        uuid,
                        device,
                        interval,
                        medal.Medal.Target_id
                    ),
                    cookie.ToString()
                );
                diagnostic?.Heartbeat(
                    sequence,
                    interval,
                    (long)(heartbeatStarted - due).TotalMilliseconds,
                    (long)(_clock.GetUtcNow() - heartbeatStarted).TotalMilliseconds,
                    response.Code,
                    response.Code == 0 && response.Data is not null
                );
                if (response.Code != 0 || response.Data is null)
                {
                    if (options.UseLiveStateMonitoring && response.Code != 0)
                        _executionGate.ReleaseWatchReservation(
                            cookie.UserId,
                            medal.Medal.Target_id,
                            interval,
                            reservationDate
                        );
                    logger.LogWarning("直播观看心跳未被接受，错误码 {Code}", response.Code);
                    if (++failures >= Math.Clamp(options.HeartBeatSendGiveUpThreshold, 1, 10))
                        throw new BiliBusinessException(
                            $"直播观看心跳失败，错误码 {response.Code}：{response.Message}。重试 {failures} 次后停止"
                        );
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Running,
                        $"心跳被拒绝，错误码 {response.Code}。正在重建会话 {failures}/{options.HeartBeatSendGiveUpThreshold}",
                        watched,
                        target,
                        confirmed
                    );
                    // A rejected heartbeat may invalidate its timestamp or session state.
                    state = await EnterAsync();
                    sequence = 1;
                    continue;
                }
                state = response.Data;
                failures = 0;
                sequence++;
                watched += interval;
                ReportMedal(
                    cookie,
                    medal,
                    "watchLive",
                    TaskRecoveryProgressState.Running,
                    "本次心跳已被接受",
                    watched,
                    target,
                    confirmed
                );
                logger.LogInformation("直播观看心跳已接受，本次累计 {Seconds} 秒", watched);
                if (watched - lastCheck >= 60 || watched >= target)
                {
                    var data = await GetTasksAsync(cookie, medal, token);
                    confirmed = data;
                    var progress = LiveFansMedalTaskPlanner.Plan(data, "watchLive");
                    var remaining = progress.Remaining;
                    diagnostic?.Progress(
                        progress.Completed,
                        data.Task_info.FirstOrDefault(task =>
                            task.Jump_type == "watchLive"
                        )?.Is_done == true,
                        data.Reach_free_intimacy_limit
                    );
                    LogProgress(medal, data, "watchLive");
                    lastCheck = watched;
                    if (remaining == 0)
                    {
                        diagnostic?.Finish("completed");
                        ReportMedal(
                            cookie,
                            medal,
                            "watchLive",
                            TaskRecoveryProgressState.Completed,
                            "平台已确认观看任务完成",
                            watched,
                            target,
                            data
                        );
                        return;
                    }
                    ReportMedal(
                        cookie,
                        medal,
                        "watchLive",
                        TaskRecoveryProgressState.Running,
                        "已更新 B 站确认的观看进度",
                        watched,
                        target,
                        data
                    );
                    if (FollowDaily && watched >= target)
                    {
                        await _delay(TimeSpan.FromSeconds(3), token);
                        data = await GetTasksAsync(cookie, medal, token);
                        progress = LiveFansMedalTaskPlanner.Plan(data, "watchLive");
                        diagnostic?.Progress(
                            progress.Completed,
                            data.Task_info.FirstOrDefault(task =>
                                task.Jump_type == "watchLive"
                            )?.Is_done == true,
                            data.Reach_free_intimacy_limit
                        );
                        if (progress.Remaining > 0)
                            throw new BiliBusinessException("观看时长已达到目标，B 站任务尚未完成");
                        diagnostic?.Finish("completed");
                        ReportMedal(
                            cookie,
                            medal,
                            "watchLive",
                            TaskRecoveryProgressState.Completed,
                            "平台已确认观看任务完成",
                            watched,
                            target,
                            data
                        );
                        return;
                    }
                }
            }
            ReportMedal(
                cookie,
                medal,
                "watchLive",
                TaskRecoveryProgressState.Pending,
                "本轮心跳时长已发送完，平台任务尚未确认完成",
                watched,
                target,
                confirmed
            );
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            diagnostic?.Finish("canceled");
            throw;
        }
        catch (Exception error)
        {
            diagnostic?.Finish(
                error is HttpRequestException ? "transport_error" : "execution_error"
            );
            throw;
        }
    }

    private static string ActionName(string action) =>
        action switch
        {
            "like" => "点赞",
            "sendDanmu" => "发送弹幕",
            "watchLive" => "观看直播",
            _ => action,
        };

    private string ConfiguredLimitMessage(
        string action,
        BiliCookie cookie,
        FansMedalPanelItem medal
    )
    {
        var amount = options.GetInteractionLimit(action);
        var limit = action == "watchLive" ? $"{amount / 60} 分钟" : $"{amount} 次";
        if (options.UseLiveStateMonitoring)
        {
            var progress = _executionGate.Progress(cookie.UserId, medal.Medal.Target_id, action);
            if (progress.Confirmed < amount && progress.Pending > 0)
                return $"已发送的{ActionName(action)}等待 B 站确认，今日已确认 {progress.Confirmed} / {amount} {(action == "watchLive" ? "秒" : "次")}。稍后补做会先核对最新进度";
            if (
                action is "sendDanmu" or "watchLive"
                && _executionGate.RemainingProtection(cookie.UserId, medal.Medal.Target_id, action)
                    == 0
            )
                return $"工具的今日{ActionName(action)}发送保护额度已用完，B 站任务尚未确认完成";
        }
        var name =
            action == "watchLive" ? "观看"
            : action == "like" ? "点赞"
            : "弹幕";
        return $"已达到面板设置的每日{name}上限（{limit}），B 站任务尚未完成。可在「互动设置」调整";
    }

    private static void ReportMedal(
        BiliCookie cookie,
        FansMedalPanelItem medal,
        string action,
        TaskRecoveryProgressState state,
        string detail,
        int? current = null,
        int? total = null,
        ActivatedMedalResponse? data = null
    )
    {
        var task = data?.Task_info.FirstOrDefault(item => item.Jump_type == action);
        var label = string.IsNullOrWhiteSpace(medal.Anchor_info.Nick_name)
            ? medal.Medal.Medal_name
            : $"{medal.Anchor_info.Nick_name}（{medal.Medal.Medal_name}）";
        TaskRecoveryProgressScope.Report(
            $"medal/{medal.Medal.Target_id}/{action}",
            $"{label} · {ActionName(action)}",
            state,
            detail,
            current,
            total,
            action == "watchLive" ? "秒" : "次",
            task is null
                ? null
                : $"{task.Title} · {task.Sub_title} · {(task.Is_done || state == TaskRecoveryProgressState.Completed ? "已完成" : "未完成")}"
        );
    }

    private void LogProgress(FansMedalPanelItem medal, ActivatedMedalResponse data, string action)
    {
        var task = data.Task_info.FirstOrDefault(item => item.Jump_type == action);
        logger.LogInformation(
            "粉丝牌 {medal} 的任务进度：{progress}，完成状态：{done}",
            medal.Medal.Medal_name,
            task?.Sub_title ?? "",
            task?.Is_done ?? false
        );
    }

    private static T Require<T>(BiliApiResponse<T> response, string operation)
        where T : class
    {
        if (response.Code != 0 || response.Data is null)
            throw new BiliBusinessException($"{operation}失败，错误码 {response.Code}");
        return response.Data;
    }
}
