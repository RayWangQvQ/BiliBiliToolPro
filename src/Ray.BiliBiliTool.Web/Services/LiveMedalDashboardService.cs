using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Web.Services;

public sealed record LiveMedalAccount(int Index, string Label);

public sealed record LiveMedalTaskProgress(
    string Action,
    string Title,
    string Progress,
    bool Done,
    double? Percent
)
{
    public static LiveMedalTaskProgress From(FansMedalTaskInfo task)
    {
        var match = Regex.Match(task.Sub_title, @"(\d+(?:\.\d+)?)\s*/\s*(\d+(?:\.\d+)?)");
        double? percent = null;
        if (
            match.Success
            && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var current)
            && double.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var total)
            && total > 0
        )
            percent = Math.Clamp(current / total * 100, 0, 100);
        return new(
            task.Jump_type,
            task.Title,
            task.Sub_title,
            task.Is_done,
            task.Is_done ? 100 : percent
        );
    }
}

public sealed record LiveMedalCard(
    long AnchorId,
    string AnchorName,
    string MedalName,
    int Level,
    bool Live,
    bool? Lighted,
    bool SavingsFull,
    IReadOnlyList<LiveMedalTaskProgress> Tasks,
    string? Error
);

public sealed record LiveMedalSnapshot(
    IReadOnlyList<LiveMedalCard> Medals,
    DateTimeOffset UpdatedAt,
    string? Error = null
);

public interface ILiveMedalDashboardService
{
    IReadOnlyList<LiveMedalAccount> GetAccounts();
    Task<LiveMedalSnapshot> GetAsync(
        int accountIndex,
        bool refresh = false,
        CancellationToken token = default
    );
}

public sealed class LiveMedalDashboardService(
    CookieStrFactory<BiliCookie> cookies,
    ILiveApi api,
    IMemoryCache cache,
    ILogger<LiveMedalDashboardService> logger
) : ILiveMedalDashboardService
{
    public IReadOnlyList<LiveMedalAccount> GetAccounts() =>
        Enumerable
            .Range(0, cookies.Count)
            .Select(index => new LiveMedalAccount(index, $"账号 {index + 1}"))
            .ToList();

    public async Task<LiveMedalSnapshot> GetAsync(
        int accountIndex,
        bool refresh = false,
        CancellationToken token = default
    )
    {
        // Cache follows both the login credentials and the Bilibili calendar day.
        var cookie = cookies.GetCookie(accountIndex);
        var fingerprint = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(cookie.ToString()))
        );
        var key =
            $"live-medals:{fingerprint}:{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)):yyyyMMdd}";
        if (!refresh && cache.TryGetValue<LiveMedalSnapshot>(key, out var cached))
            return cached!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var medals = new Dictionary<long, FansMedalPanelItem>();
            for (var page = 1; ; page++)
            {
                var data = Require(
                    await api.GetFansMedalPanel(page, cookie.ToString(), timeout.Token)
                );
                foreach (var medal in data.Special_list.Concat(data.List))
                    if (medal.Medal.Target_id > 0)
                        medals.TryAdd(medal.Medal.Target_id, medal);
                if (!data.Page_info.Has_more && page >= data.Page_info.Total_page)
                    break;
                if (page >= 1000)
                    throw new InvalidOperationException("粉丝牌列表暂未加载完成，请刷新");
                await Task.Delay(200, timeout.Token);
            }

            using var slots = new SemaphoreSlim(2);
            async Task<LiveMedalCard> ReadAsync(FansMedalPanelItem medal)
            {
                var acquired = false;
                try
                {
                    await slots.WaitAsync(timeout.Token);
                    acquired = true;
                    await Task.Delay(150, timeout.Token);
                    var tasks = Require(
                        await api.GetActivatedMedalInfo(
                            medal.Medal.Target_id,
                            cookie.BiliJct,
                            cookie.ToString(),
                            timeout.Token
                        )
                    );
                    return Card(medal, tasks, null);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogDebug("Medal progress read failed: {ErrorType}", ex.GetType().Name);
                    return Card(medal, null, SafeMessage(ex));
                }
                finally
                {
                    if (acquired)
                        slots.Release();
                }
            }
            var cards = await Task.WhenAll(medals.Values.Select(ReadAsync));
            var result = new LiveMedalSnapshot(
                cards
                    .OrderByDescending(card => card.Level)
                    .ThenBy(card => card.AnchorName)
                    .ToList(),
                DateTimeOffset.UtcNow
            );
            if (cards.All(card => card.Error is null))
                cache.Set(key, result, TimeSpan.FromSeconds(60));
            return result;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug("Medal panel read failed: {ErrorType}", ex.GetType().Name);
            return new([], DateTimeOffset.UtcNow, SafeMessage(ex));
        }
    }

    private static LiveMedalCard Card(
        FansMedalPanelItem medal,
        ActivatedMedalResponse? tasks,
        string? error
    ) =>
        new(
            medal.Medal.Target_id,
            string.IsNullOrWhiteSpace(medal.Anchor_info.Nick_name)
                ? $"主播 {medal.Medal.Target_id}"
                : medal.Anchor_info.Nick_name,
            medal.Medal.Medal_name,
            tasks?.Level > 0 ? tasks.Level : medal.Medal.Level,
            medal.Room_info.Living_status == 1,
            tasks?.Is_lighted,
            tasks?.Reach_free_intimacy_limit ?? false,
            tasks?.Task_info.Select(LiveMedalTaskProgress.From).ToList() ?? [],
            error
        );

    private static T Require<T>(BiliApiResponse<T> response)
        where T : class
    {
        if (response.Code == -101)
            throw new LoginRequiredException();
        if (response.Code != 0 || response.Data is null)
            throw new InvalidOperationException();
        return response.Data;
    }

    private static string SafeMessage(Exception error) =>
        error is LoginRequiredException ? "请重新登录账号后刷新" : "任务进度暂未加载，请刷新";

    private sealed class LoginRequiredException : Exception { }
}
