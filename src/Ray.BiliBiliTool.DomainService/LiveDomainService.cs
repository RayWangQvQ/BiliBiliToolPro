using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveTraceApi;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Extensions;
using UpInfoDto = Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.ApiApi.UpInfo.UpInfo;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 直播
/// </summary>
public class LiveDomainService(
    ILogger<LiveDomainService> logger,
    ILiveApi liveApi,
    IApiApi apiApi,
    ILiveTraceApi liveTraceApi,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
    IOptionsMonitor<LiveFansMedalTaskOptions> liveFansMedalTaskOptions,
    IOptionsMonitor<SecurityOptions> securityOptions,
    IOptionsMonitor<Silver2CoinTaskOptions> silver2CoinTaskOptions,
    LiveFansMedalExecutionGate? executionGate = null,
    ILiveFansMedalProgressObserver? progressObserver = null,
    LiveWatchDiagnostics? watchDiagnostics = null
) : ILiveDomainService
{
    private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions =
        liveLotteryTaskOptions.CurrentValue;
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly SecurityOptions _securityOptions = securityOptions.CurrentValue;
    private readonly Silver2CoinTaskOptions _silver2CoinTaskOptions =
        silver2CoinTaskOptions.CurrentValue;

    /// <summary>
    /// 本次通过天选关注的主播
    /// </summary>
    private List<ListItemDto> _tianXuanFollowed = new();

    /// <summary>
    /// 开始抽奖前最后一个关注的up
    /// </summary>
    private long _lastFollowUpId;

    /// <summary>
    /// 直播签到
    /// </summary>
    public async Task LiveSign(BiliCookie ck)
    {
        var response = await liveApi.Sign(ck.ToString());

        if (response.Code == 0)
        {
            logger.LogInformation("【签到结果】成功");
            logger.LogInformation(
                "【本次获取】{text},{special}",
                response.Data!.Text,
                response.Data.SpecialText
            );
        }
        else
        {
            logger.LogInformation("【签到结果】失败");
            logger.LogInformation("【原因】{msg}", response.Message);
        }
    }

    /// <summary>
    /// 直播中心银瓜子兑换B币
    /// </summary>
    /// <returns>兑换银瓜子后硬币余额</returns>
    public async Task<bool> ExchangeSilver2Coin(BiliCookie ck)
    {
        var result = false;

        if (!_silver2CoinTaskOptions.IsEnable)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return false;
        }

        logger.LogInformation("【今天】{day}号", DateTime.Today.Day);

        BiliApiResponse<LiveWalletStatusResponse> queryStatus = await liveApi.GetLiveWalletStatus(
            ck.ToString()
        );
        if (queryStatus.Code != 0 || queryStatus.Data is null)
        {
            logger.LogWarning(
                "获取直播钱包信息失败：{message}({code})",
                queryStatus.Message,
                queryStatus.Code
            );
            return false;
        }

        logger.LogInformation("【银瓜子余额】 {silver}", queryStatus.Data.Silver);
        logger.LogInformation("【硬币余额】 {coin}", queryStatus.Data.Coin);
        logger.LogInformation("【今日剩余兑换次数】 {left}", queryStatus.Data.Silver_2_coin_left);

        if (queryStatus.Data.Silver_2_coin_left <= 0)
            return false;

        logger.LogInformation("开始尝试兑换...");
        Silver2CoinRequest request = new(ck.BiliJct);
        var response = await liveApi.Silver2Coin(request, ck.ToString());
        if (response.Code == 0)
        {
            result = true;
            logger.LogInformation("【兑换结果】成功兑换 {coin} 枚硬币", response.Data?.Coin);
            logger.LogInformation("【银瓜子余额】 {silver}", response.Data?.Silver);
        }
        else
        {
            logger.LogInformation("【兑换结果】失败");
            logger.LogInformation("【原因】{reason}", response.Message);
        }

        return result;
    }

    #region 天选时刻抽奖

    /// <summary>
    /// 天选抽奖
    /// </summary>
    public async Task TianXuan(BiliCookie ck)
    {
        _tianXuanFollowed = new List<ListItemDto>();

        if (_liveLotteryTaskOptions.AutoGroupFollowings)
        {
            //获取此时最后一个关注的up，此后再新增的关注，与参与成功的抽奖，取交集，就是本地新增的天选关注
            _lastFollowUpId = await GetLastFollowUpId(ck);
        }

        //获取直播的分区
        BiliApiResponse<GetArteaListResponse> areaResponse = await liveApi.GetAreaList(
            ck.ToString()
        );
        if (areaResponse.Code != 0 || areaResponse.Data?.Data is null)
        {
            logger.LogWarning(
                "获取直播分区失败：{message}({code})",
                areaResponse.Message,
                areaResponse.Code
            );
            return;
        }

        List<AreaDto> areaList = areaResponse.Data.Data;

        //遍历分区
        int count = 0;
        foreach (var area in areaList)
        {
            logger.LogInformation("【扫描分区】{area}..." + Environment.NewLine, area.Name);

            string defaultSort = "";
            //每个分区下搜索5页
            for (int i = 1; i < 6; i++)
            {
                var request = new GetListRequest
                {
                    platform = "web",
                    parent_area_id = area.Id,
                    area_id = 0,
                    sort_type = defaultSort,
                    page = i,
                    wts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                };
                BiliApiResponse<GetListResponse> listResponse = await liveApi.GetList(
                    request,
                    ck.ToString()
                );
                if (listResponse.Code != 0 || listResponse.Data is null)
                {
                    logger.LogWarning(
                        "获取分区直播间列表失败：{message}({code})",
                        listResponse.Message,
                        listResponse.Code
                    );
                    return;
                }

                var reData = listResponse.Data;

                foreach (var item in reData.List)
                {
                    if (item.Pendant_info == null || item.Pendant_info.Count == 0)
                        continue;
                    var suc = item.Pendant_info.TryGetValue("2", out var pendant);
                    if (!suc)
                        continue;
                    if (pendant?.Pendent_id != 504)
                        continue;
                    count++;

                    await TryJoinTianXuan(item, ck);
                }

                if (reData.Has_more != 1)
                    break;
                defaultSort = reData.New_tags.FirstOrDefault()?.Sort_type ?? "";
            }

            defaultSort = "";
        }

        if (count == 0)
        {
            logger.LogInformation("未搜索到直播间");
            return;
        }
    }

    public async Task TryJoinTianXuan(ListItemDto target, BiliCookie ck)
    {
        logger.LogDebug("【房间】{name}", target.Title);
        try
        {
            //黑名单
            if (_liveLotteryTaskOptions.DenyUidList.Contains(target.Uid.ToString()))
            {
                logger.LogDebug("黑名单，跳过");
                return;
            }

            CheckTianXuanDto? check = (
                await liveApi.CheckTianXuan(target.Roomid, ck.ToString())
            ).Data;

            if (check == null)
            {
                logger.LogDebug("数据异常，跳过");
                return;
            }

            if (check.Status != TianXuanStatus.Enable)
            {
                logger.LogDebug("已开奖，跳过" + Environment.NewLine);
                return;
            }

            //根据配置过滤
            if (
                !check.AwardNameIsSatisfied(
                    _liveLotteryTaskOptions.IncludeAwardNameList,
                    _liveLotteryTaskOptions.ExcludeAwardNameList
                )
            )
            {
                logger.LogDebug("不满足配置的筛选条件，跳过" + Environment.NewLine);
                return;
            }

            //是否需要赠礼
            if (check.Gift_price > 0)
            {
                logger.LogDebug("【赠礼】{gift}", check.GiftDesc);
                logger.LogDebug("需赠送礼物，跳过" + Environment.NewLine);
                return;
            }

            //条件
            if (check.Require_type != RequireType.None && check.Require_type != RequireType.Follow)
            {
                logger.LogDebug("【条件】{text}", check.Require_text);
                logger.LogDebug("要求粉丝勋章，跳过");
                return;
            }

            logger.LogInformation("【房间】{name}", target.ShortTitle);
            logger.LogInformation("【主播】{name}({id})", target.Uname, target.Uid);
            logger.LogInformation(
                "【奖品】{name}【条件】{text}",
                check.Award_name,
                check.Require_text
            );

            var request = new JoinTianXuanRequest
            {
                Id = check.Id,
                Gift_id = check.Gift_id,
                Gift_num = check.Gift_num,
                Csrf = ck.BiliJct,
            };
            var re = await liveApi.Join(request, ck.ToString());
            if (re.Code == 0)
            {
                logger.LogInformation("【抽奖】成功 √" + Environment.NewLine);
                if (check.Require_type == RequireType.Follow)
                    _tianXuanFollowed.AddIfNotExist(target, x => x.Uid == target.Uid);
                return;
            }

            logger.LogInformation("【抽奖】失败");
            logger.LogInformation("【原因】{msg}" + Environment.NewLine, re.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning("【异常】{msg}，{detail}" + Environment.NewLine, ex.Message, ex);
            //ignore
        }
    }

    /// <summary>
    /// 将本次抽奖新增的关注统一转移到指定分组中
    /// </summary>
    public async Task GroupFollowing(BiliCookie ck)
    {
        if (!_tianXuanFollowed.Any())
        {
            logger.LogInformation("未关注主播");
            return;
        }

        logger.LogInformation(
            "【抽奖的主播】{ups}",
            string.Join("，", _tianXuanFollowed.Select(x => x.Uname))
        );

        //目标分组up集合
        List<ListItemDto> targetUps = await GetNeedGroup(ck);
        logger.LogInformation(
            "【将自动分组】{ups}",
            string.Join("，", targetUps.Select(x => x.Uname))
        );

        if (!targetUps.Any())
        {
            return;
        }

        //目标分组Id
        long targetGroupId = await GetOrCreateTianXuanGroupId(ck);

        //执行批量分组
        var referer = string.Format(RelationApiConstant.CopyReferer, ck.UserId);
        var req = new CopyUserToGroupRequest(
            targetUps.Select(x => x.Uid).ToList(),
            targetGroupId.ToString(),
            ck.BiliJct
        );
        var re = await apiApi.CopyUpsToGroup(req, ck.ToString(), referer);

        if (re.Code == 0)
        {
            logger.LogInformation("【分组结果】全部成功");
        }
        else
        {
            throw new BiliBusinessException(
                $"移动关注到“天选时刻”分组失败：{re.Message}（错误码 {re.Code}）"
            );
        }
    }

    /// <summary>
    /// 获取抽奖前最后一个关注的up
    /// </summary>
    /// <returns></returns>
    private async Task<long> GetLastFollowUpId(BiliCookie ck)
    {
        var followings = await GetFollowingList(ck);
        return followings.FirstOrDefault()?.Mid ?? 0;
    }

    private async Task<List<UpInfoDto>> GetFollowingList(BiliCookie ck)
    {
        var followings = await apiApi.GetFollowings(
            new GetFollowingsRequest(long.Parse(ck.UserId), FollowingsOrderType.TimeDesc),
            ck.ToString()
        );
        if (followings.Code != 0)
            throw new BiliBusinessException(
                $"获取关注列表失败：{followings.Message}（错误码 {followings.Code}）"
            );
        if (followings.Data?.List is null || followings.Data.List.Any(item => item is null))
            throw new BiliBusinessException("获取关注列表未返回有效的关注数据");
        return followings.Data.List;
    }

    /// <summary>
    /// 获取本次需要自动分组的主播
    /// </summary>
    /// <returns></returns>
    private async Task<List<ListItemDto>> GetNeedGroup(BiliCookie ck)
    {
        List<long> addUpIds = new();

        //获取最后一个upId之后关注的所有upId
        var followings = await GetFollowingList(ck);

        foreach (UpInfoDto item in followings)
        {
            if (item.Mid == _lastFollowUpId)
            {
                break;
            }

            addUpIds.Add(item.Mid);
        }

        //和成功抽奖的主播取交集
        List<ListItemDto> target = new();
        foreach (var listItemDto in _tianXuanFollowed)
        {
            if (addUpIds.Contains(listItemDto.Uid))
                target.Add(listItemDto);
        }

        return target;
    }

    /// <summary>
    /// 获取或创建天选时刻分组
    /// </summary>
    /// <returns></returns>
    private async Task<long> GetOrCreateTianXuanGroupId(BiliCookie ck)
    {
        //获取天选分组Id，没有就创建
        long groupId = 0;
        string referer = string.Format(RelationApiConstant.GetTagsReferer, ck.UserId);
        var groups = await apiApi.GetTags(ck.ToString(), referer);
        if (groups.Code != 0)
            throw new BiliBusinessException(
                $"获取关注分组失败：{groups.Message}（错误码 {groups.Code}）"
            );
        if (groups.Data is null || groups.Data.Any(group => group is null))
            throw new BiliBusinessException("获取关注分组未返回有效的分组数据");
        var tianXuanGroup = groups.Data.FirstOrDefault(x => x.Name == "天选时刻");
        if (tianXuanGroup == null)
        {
            logger.LogInformation("“天选时刻”分组不存在，尝试创建...");
            //创建一个
            var createRe = await apiApi.CreateTag(
                new CreateTagRequest { Tag = "天选时刻", Csrf = ck.BiliJct },
                ck.ToString(),
                referer
            );
            if (createRe.Code != 0)
                throw new BiliBusinessException(
                    $"创建“天选时刻”分组失败：{createRe.Message}（错误码 {createRe.Code}）"
                );
            if (createRe.Data is null || createRe.Data.Tagid <= 0)
                throw new BiliBusinessException("创建“天选时刻”分组未返回有效的分组编号");

            groupId = createRe.Data.Tagid;
            logger.LogInformation("创建成功");
        }
        else
        {
            if (tianXuanGroup.Tagid <= 0)
                throw new BiliBusinessException("“天选时刻”分组编号无效");
            logger.LogInformation("“天选时刻”分组已存在");
            groupId = tianXuanGroup.Tagid;
        }

        return groupId;
    }

    #endregion

    public async Task RunFansMedalActionForAnchorAsync(
        BiliCookie cookie,
        long anchorId,
        long roomId,
        string action,
        CancellationToken cancellationToken = default
    )
    {
        if (!liveFansMedalTaskOptions.CurrentValue.IsEnable)
            return;
        cancellationToken.ThrowIfCancellationRequested();
        if (action == "watchLive" && !await CheckLiveCookie(cookie, cancellationToken))
            throw new BiliBusinessException("直播设备信息暂未获取，观看任务稍后重试");
        var runner = new LiveFansMedalTaskRunner(
            liveApi,
            liveTraceApi,
            logger,
            liveFansMedalTaskOptions.CurrentValue,
            securityOptions.CurrentValue.UserAgent,
            executionGate: executionGate,
            progressObserver: progressObserver,
            watchDiagnostics: watchDiagnostics
        );
        await runner.RunForAnchorAsync(cookie, anchorId, roomId, action, cancellationToken);
    }

    public Task SendDanmakuToFansMedalLive(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    ) => RunFansMedalTaskAsync(ck, "sendDanmu", cancellationToken);

    public Task SendHeartBeatToFansMedalLive(
        BiliCookie ck,
        CancellationToken cancellationToken = default
    ) => RunFansMedalTaskAsync(ck, "watchLive", cancellationToken);

    public Task LikeFansMedalLive(BiliCookie ck, CancellationToken cancellationToken = default) =>
        RunFansMedalTaskAsync(ck, "like", cancellationToken);

    private async Task RunFansMedalTaskAsync(
        BiliCookie ck,
        string action,
        CancellationToken cancellationToken
    )
    {
        if (!liveFansMedalTaskOptions.CurrentValue.IsEnable)
            return;
        cancellationToken.ThrowIfCancellationRequested();
        if (action == "watchLive" && !await CheckLiveCookie(ck, cancellationToken))
            throw new BiliBusinessException("直播设备信息暂未获取，观看任务稍后重试");
        var runner = new LiveFansMedalTaskRunner(
            liveApi,
            liveTraceApi,
            logger,
            liveFansMedalTaskOptions.CurrentValue,
            securityOptions.CurrentValue.UserAgent,
            executionGate: executionGate,
            progressObserver: progressObserver,
            watchDiagnostics: watchDiagnostics
        );
        await runner.RunAsync(ck, action, cancellationToken);
    }

    /// <summary>
    /// 自动配置直播相关 Cookie，来兼容较低版本中保存的 Cookie 配置
    /// </summary>
    /// <returns>
    /// bool 成功配置 or not
    /// </returns>
    private async Task<bool> CheckLiveCookie(BiliCookie ck, CancellationToken token)
    {
        // 检测 _biliCookie 是否正确配置
        if (!string.IsNullOrWhiteSpace(ck.LiveBuvid))
            return true;

        try
        {
            logger.LogInformation("检测到直播 Cookie 未正确配置，尝试自动配置中...");

            // The API may omit Set-Cookie for an authenticated request. Acquire only the
            // live device cookie anonymously when needed, preserving all login credentials.
            foreach (var requestCookie in new[] { ck.ToString(), "" })
            {
                token.ThrowIfCancellationRequested();
                using var liveHome = await AwaitLiveHomeResponseAsync(
                    liveApi.GetLiveHome(requestCookie),
                    token
                );
                token.ThrowIfCancellationRequested();
                liveHome.EnsureSuccessStatusCode();
                var liveHomeContent = JsonConvert.DeserializeObject<BiliApiResponse>(
                    await liveHome.Content.ReadAsStringAsync(token)
                );
                token.ThrowIfCancellationRequested();
                if (liveHomeContent?.Code != 0)
                    throw new BiliBusinessException(
                        $"直播设备初始化失败，错误码 {liveHomeContent?.Code}"
                    );
                if (liveHome.Headers.TryGetValues("Set-Cookie", out var headers))
                    ck.MergeCurrentCookieBySetCookieHeaders(
                        headers.Where(header =>
                            header.TrimStart().StartsWith("LIVE_BUVID=", StringComparison.Ordinal)
                        )
                    );
                if (!string.IsNullOrWhiteSpace(ck.LiveBuvid))
                {
                    logger.LogInformation("直播设备信息配置成功");
                    return true;
                }
            }
            logger.LogWarning("直播设备信息暂未获取");
            return false;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Live device initialization failed: {ErrorType}",
                exception.GetType().Name
            );
            return false;
        }
    }

    private static async Task<HttpResponseMessage> AwaitLiveHomeResponseAsync(
        Task<HttpResponseMessage> pending,
        CancellationToken token
    )
    {
        try
        {
            return await pending.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The legacy API cannot cancel its request. Dispose a late response and observe failures.
            _ = DisposeLateLiveHomeResponseAsync(pending);
            throw;
        }
    }

    private static async Task DisposeLateLiveHomeResponseAsync(Task<HttpResponseMessage> pending)
    {
        try
        {
            using var response = await pending.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The caller has already received its cancellation.
        }
    }
}
