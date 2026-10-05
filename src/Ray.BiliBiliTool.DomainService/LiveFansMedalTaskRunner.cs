using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;

namespace Ray.BiliBiliTool.DomainService;

public class LiveFansMedalTaskRunner(
    ILiveApi liveApi,
    ILiveTraceApi traceApi,
    ILogger logger,
    LiveFansMedalTaskOptions options,
    string userAgent,
    Func<TimeSpan, CancellationToken, Task>? delay = null
)
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;
    private int _likesSent;

    public async Task RunAsync(BiliCookie cookie, string action, CancellationToken token = default)
    {
        var enabled = action switch
        {
            "like" => options.EnableLike,
            "sendDanmu" => options.EnableDanmaku,
            "watchLive" => options.EnableWatch,
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
        var customBudget = action switch
        {
            "like" => Math.Clamp(options.LikeNumber, 0, 5000),
            "sendDanmu" => Math.Clamp(options.SendDanmakuNumber, 0, 100),
            _ => Math.Clamp(options.HeartBeatNumber, 0, 1440) * 60,
        };
        if (!enabled || (!options.FollowDailyTaskLimit && customBudget == 0))
            return;

        var medals = await GetMedalsAsync(cookie, token);
        var failures = 0;
        // Limit concurrent watch sessions while retaining parallel watching.
        using var watchSlots = new SemaphoreSlim(8);
        async Task ExecuteAsync(FansMedalPanelItem medal)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var data = await GetTasksAsync(cookie, medal, token);
                var plan = LiveFansMedalTaskPlanner.Plan(data, action);
                var target = Math.Min(
                    plan.Remaining,
                    options.FollowDailyTaskLimit
                        ? action == "like"
                            ? 5000
                            : action == "sendDanmu"
                                ? 100
                                : 86400
                        : customBudget
                );
                if (action == "like")
                    target = Math.Min(target, 5000 - _likesSent);
                if (target <= 0)
                    return;

                if (action == "watchLive")
                {
                    await watchSlots.WaitAsync(token);
                    try
                    {
                        await WatchAsync(cookie, medal, target, token);
                    }
                    finally
                    {
                        watchSlots.Release();
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
    }

    private async Task<List<FansMedalPanelItem>> GetMedalsAsync(
        BiliCookie cookie,
        CancellationToken token
    )
    {
        var result = new Dictionary<long, FansMedalPanelItem>();
        var excluded = options.GetExcludedAnchorIds();
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
    ) =>
        Require(
            await liveApi.GetActivatedMedalInfo(
                medal.Medal.Target_id,
                cookie.BiliJct,
                cookie.ToString(),
                token
            ),
            "获取粉丝牌任务进度"
        );

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
                    return sent;
                if (
                    action == "sendDanmu"
                    && options.DanmakuOnlyWhenOffline
                    && room.Live_Status == 1
                )
                    return sent;
                // A live room can be lit with likes without additional chat messages.
                if (
                    action == "sendDanmu"
                    && !data.Is_lighted
                    && room.Live_Status == 1
                    && options.EnableLike
                )
                    return sent;
                var amount = action == "like" ? Math.Min(10, roundTarget - roundSent) : 1;
                if (action == "like")
                {
                    amount = Math.Min(amount, 5000 - _likesSent);
                    if (amount <= 0)
                        return sent;
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
                    failures++;
                    var threshold =
                        action == "like"
                            ? 3
                            : Math.Clamp(options.SendDanmakugiveUpThreshold, 1, 10);
                    if (failures >= threshold)
                        throw new BiliBusinessException($"粉丝牌互动失败，错误码 {response.Code}");
                }
                else
                {
                    sent += amount;
                    roundSent += amount;
                    failures = 0;
                    if (action == "like")
                        _likesSent += amount;
                }
                await _delay(TimeSpan.FromSeconds(action == "like" ? 3 : 7), token);
            }

            var wasLighted = data.Is_lighted;
            data = await GetTasksAsync(cookie, medal, token);
            var latest = LiveFansMedalTaskPlanner.Plan(data, action);
            LogProgress(medal, data, action);
            // A lighting task changes into a different daily task after lighting.
            if (!wasLighted && data.Is_lighted)
            {
                if (!options.FollowDailyTaskLimit)
                    return sent;
                target = Math.Min(action == "like" ? 5000 : 100, sent + latest.Remaining);
                previous = latest;
                continue;
            }
            if (roundSent >= previous.RoundSize && latest.Remaining >= previous.Remaining)
            {
                await _delay(TimeSpan.FromSeconds(3), token);
                data = await GetTasksAsync(cookie, medal, token);
                latest = LiveFansMedalTaskPlanner.Plan(data, action);
                if (latest.Remaining >= previous.Remaining)
                    throw new BiliBusinessException("互动已发送，B 站任务进度尚未更新");
            }
            previous = latest;
        }
        return sent;
    }

    private async Task WatchAsync(
        BiliCookie cookie,
        FansMedalPanelItem medal,
        int target,
        CancellationToken token
    )
    {
        var roomId = medal.Room_info.Room_id;
        var room = Require(await liveApi.GetLiveRoomInfo(roomId), "获取直播状态");
        if (room.Live_Status != 1 || room.Parent_area_id <= 0)
            return;
        var uuid = Guid.NewGuid().ToString();
        var device = $"[\"{cookie.LiveBuvid}\",\"{uuid}\"]";
        var state = Require(
            await traceApi.EnterRoom(
                new EnterRoomRequest(
                    roomId,
                    room.Parent_area_id,
                    room.Area_id,
                    0,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    userAgent,
                    cookie.BiliJct,
                    medal.Medal.Target_id,
                    device
                ),
                cookie.ToString()
            ),
            "进入直播间"
        );
        var watched = 0;
        var sequence = 1;
        var failures = 0;
        var lastCheck = 0;
        while (watched < target)
        {
            if (
                state.Heartbeat_interval <= 0
                || state.Heartbeat_interval > 300
                || string.IsNullOrEmpty(state.Secret_key)
            )
                throw new BiliBusinessException("直播观看任务返回了无效的心跳参数");
            var interval = state.Heartbeat_interval;
            await _delay(TimeSpan.FromSeconds(interval), token);
            room = Require(await liveApi.GetLiveRoomInfo(roomId), "获取直播状态");
            if (room.Live_Status != 1)
                return;
            var response = await traceApi.HeartBeat(
                new HeartBeatRequest(
                    roomId,
                    room.Parent_area_id,
                    room.Area_id,
                    sequence,
                    cookie.LiveBuvid,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
            if (response.Code != 0 || response.Data is null)
            {
                if (++failures >= Math.Clamp(options.HeartBeatSendGiveUpThreshold, 1, 10))
                    throw new BiliBusinessException($"直播观看心跳失败，错误码 {response.Code}");
                continue;
            }
            state = response.Data;
            failures = 0;
            sequence++;
            watched += interval;
            if (watched - lastCheck >= 60 || watched >= target)
            {
                var data = await GetTasksAsync(cookie, medal, token);
                var remaining = LiveFansMedalTaskPlanner.Plan(data, "watchLive").Remaining;
                LogProgress(medal, data, "watchLive");
                lastCheck = watched;
                if (remaining == 0)
                    return;
                if (options.FollowDailyTaskLimit && watched >= target)
                {
                    await _delay(TimeSpan.FromSeconds(3), token);
                    data = await GetTasksAsync(cookie, medal, token);
                    if (LiveFansMedalTaskPlanner.Plan(data, "watchLive").Remaining > 0)
                        throw new BiliBusinessException("观看时长已达到目标，B 站任务尚未完成");
                }
            }
        }
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
