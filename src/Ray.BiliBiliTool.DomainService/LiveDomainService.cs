using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Dtos;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService;

/// <summary>
/// 直播
/// </summary>
public class LiveDomainService(
    ILogger<LiveDomainService> logger,
    ILiveApi liveApi,
    IRelationApi relationApi,
    ILiveTraceApi liveTraceApi,
    IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
    IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
    IOptionsMonitor<LiveFansMedalTaskOptions> liveFansMedalTaskOptions,
    IOptionsMonitor<SecurityOptions> securityOptions,
    IUpInfoApi upInfoApi
) : ILiveDomainService
{
    private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions =
        liveLotteryTaskOptions.CurrentValue;
    private readonly LiveFansMedalTaskOptions _liveFansMedalTaskOptions =
        liveFansMedalTaskOptions.CurrentValue;
    private readonly DailyTaskOptions _dailyTaskOptions = dailyTaskOptions.CurrentValue;
    private readonly SecurityOptions _securityOptions = securityOptions.CurrentValue;

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
                response.Data.Text,
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

        if (_dailyTaskOptions.DayOfExchangeSilver2Coin == 0)
        {
            logger.LogInformation("已配置为关闭，跳过");
            return false;
        }

        int targetDay =
            _dailyTaskOptions.DayOfExchangeSilver2Coin == -2 ? DateTime.Today.Day
            : _dailyTaskOptions.DayOfExchangeSilver2Coin == -1 ? DateTime.Today.LastDayOfMonth().Day
            : _dailyTaskOptions.DayOfExchangeSilver2Coin;

        logger.LogInformation("【目标兑换日期】{targetDay}号", targetDay);
        logger.LogInformation("【今天】{day}号", DateTime.Today.Day);

        if (DateTime.Today.Day != targetDay)
        {
            logger.LogInformation("跳过");
            return false;
        }

        BiliApiResponse<LiveWalletStatusResponse> queryStatus = await liveApi.GetLiveWalletStatus(
            ck.ToString()
        );
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
            logger.LogInformation("【兑换结果】成功兑换 {coin} 枚硬币", response.Data.Coin);
            logger.LogInformation("【银瓜子余额】 {silver}", response.Data.Silver);
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
        List<AreaDto> areaList = (await liveApi.GetAreaList(ck.ToString())).Data.Data;

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
                var reData = (await liveApi.GetList(request, ck.ToString())).Data;
                if (reData == null)
                {
                    continue;
                }
                foreach (var item in reData.List ?? new List<ListItemDto>())
                {
                    if (item.Pendant_info == null || item.Pendant_info.Count == 0)
                        continue;
                    var suc = item.Pendant_info.TryGetValue("2", out var pendant);
                    if (!suc)
                        continue;
                    if (pendant.Pendent_id != 504)
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

            CheckTianXuanDto check = (
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
        var re = await relationApi.CopyUpsToGroup(req, ck.ToString(), referer);

        if (re.Code == 0)
        {
            logger.LogInformation("【分组结果】全部成功");
        }
        else
        {
            logger.LogWarning("【分组结果】失败");
            logger.LogWarning("【原因】{msg}", re.Message);
        }
    }

    /// <summary>
    /// 获取抽奖前最后一个关注的up
    /// </summary>
    /// <returns></returns>
    private async Task<long> GetLastFollowUpId(BiliCookie ck)
    {
        var followings = await relationApi.GetFollowings(
            new GetFollowingsRequest(long.Parse(ck.UserId), FollowingsOrderType.TimeDesc),
            ck.ToString()
        );
        return followings.Data.List.FirstOrDefault()?.Mid ?? 0;
    }

    /// <summary>
    /// 获取本次需要自动分组的主播
    /// </summary>
    /// <returns></returns>
    private async Task<List<ListItemDto>> GetNeedGroup(BiliCookie ck)
    {
        List<long> addUpIds = new();

        //获取最后一个upId之后关注的所有upId
        var followings = await relationApi.GetFollowings(
            new GetFollowingsRequest(long.Parse(ck.UserId), FollowingsOrderType.TimeDesc),
            ck.ToString()
        );

        foreach (UpInfo item in followings.Data.List)
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
        var groups = await relationApi.GetTags(referer);
        var tianXuanGroup = groups.Data.FirstOrDefault(x => x.Name == "天选时刻");
        if (tianXuanGroup == null)
        {
            logger.LogInformation("“天选时刻”分组不存在，尝试创建...");
            //创建一个
            var createRe = await relationApi.CreateTag(
                new CreateTagRequest { Tag = "天选时刻", Csrf = ck.BiliJct },
                ck.ToString()
            );
            groupId = createRe.Data.Tagid;
            logger.LogInformation("创建成功");
        }
        else
        {
            logger.LogInformation("“天选时刻”分组已存在");
            groupId = tianXuanGroup.Tagid;
        }

        return groupId;
    }

    #endregion

    public async Task SendDanmakuToFansMedalLive(BiliCookie ck)
    {
        if (!await CheckLiveCookie(ck))
            return;

        var infoList = await GetFansMedalInfoList(ck);

        foreach (var info in infoList)
        {
            var medal = info.MedalInfo;

            logger.LogInformation("【直播间】{liveRoomName}", medal.Target_name);
            logger.LogInformation("【粉丝牌】{medalName}", medal.Medal_info.Medal_name);
            logger.LogInformation("正在发送弹幕...");

            // 通过空间主页信息获取直播间 id
            var liveHostUserId = medal.Medal_info.Target_id;
            var req = new GetSpaceInfoDto() { mid = liveHostUserId };

            var spaceInfo = await upInfoApi.GetSpaceInfo(req, ck.ToString());
            if (spaceInfo.Code != 0)
            {
                logger.LogError("【获取直播间信息】失败");
                logger.LogError("【原因】{message}", spaceInfo.Message);
                return;
            }

            var successCount = 0;
            var failedCount = 0;

            // 发送弹幕

            while (
                successCount < _liveFansMedalTaskOptions.SendDanmakuNumber
                && failedCount < _liveFansMedalTaskOptions.SendDanmakugiveUpThreshold
            )
            {
                var sendResult = await liveApi.SendLiveDanmuku(
                    new SendLiveDanmukuRequest(
                        ck.BiliJct,
                        spaceInfo.Data.Live_room.Roomid,
                        _liveFansMedalTaskOptions.DanmakuContent
                    ),
                    ck.ToString()
                );

                if (sendResult.Code != 0)
                {
                    logger.LogError("【弹幕发送】失败");
                    logger.LogError("【原因】{message}", sendResult.Message);
                    failedCount++;
                }
                else
                    successCount++;

                var delay = new Random().Next(2000, 4000);
                await Task.Delay(delay);
            }

            logger.LogInformation(
                "【弹幕发送】发送情况：你向主播 {name} 发送弹幕{success}/{total}",
                spaceInfo.Data.Name,
                successCount,
                successCount + failedCount
            );
        }
    }

    public async Task SendHeartBeatToFansMedalLive(BiliCookie ck)
    {
        if (!await CheckLiveCookie(ck))
            return;

        var infoList = new List<HeartBeatIterationInfoDto>();
        (await GetFansMedalInfoList(ck))
            .FindAll(info => info.LiveRoomInfo.Live_Status != 0)
            .ForEach(medal => infoList.Add(new(medal.RoomId, medal.LiveRoomInfo, new(), 0, 0)));

        if (infoList.Count == 0)
        {
            logger.LogInformation("【直播观看时长】跳过，未检测到符合条件的主播");
            return;
        }

        var Now = () => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        while (
            infoList.Min(info =>
                info.FailedTimes >= _liveFansMedalTaskOptions.HeartBeatSendGiveUpThreshold
                    ? int.MaxValue
                    : info.HeartBeatCount
            ) < _liveFansMedalTaskOptions.HeartBeatNumber
        )
        {
            foreach (var info in infoList)
            {
                // 忽略连续失败超过上限的直播间
                if (info.FailedTimes >= _liveFansMedalTaskOptions.HeartBeatSendGiveUpThreshold)
                    continue;

                string uuid = Guid.NewGuid().ToString();
                var current = Now();
                if (
                    current - info.LastBeatTime
                    <= (LiveFansMedalTaskOptions.HeartBeatInterval + 5) * 1000
                )
                {
                    int sleepTime = (int)(
                        (LiveFansMedalTaskOptions.HeartBeatInterval + 5) * 1000
                        - (current - info.LastBeatTime)
                    );
                    logger.LogDebug("【休眠】{time} 毫秒", sleepTime);
                    Thread.Sleep(sleepTime);
                }

                // Heart Beat 接口
                var timestamp = Now();
                BiliApiResponse<HeartBeatResponse> heartBeatResult = null;
                if (info.HeartBeatCount == 0)
                {
                    heartBeatResult = await liveTraceApi.EnterRoom(
                        new EnterRoomRequest(
                            info.RoomId,
                            info.RoomInfo.Parent_area_id,
                            info.RoomInfo.Area_id,
                            info.HeartBeatCount,
                            timestamp,
                            _securityOptions.UserAgent,
                            ck.BiliJct,
                            info.RoomInfo.Uid,
                            $"[\"{ck.LiveBuvid}\",\"{uuid}\"]"
                        ),
                        ck.ToString()
                    );
                }
                else
                {
                    heartBeatResult = await liveTraceApi.HeartBeat(
                        new HeartBeatRequest(
                            info.RoomId,
                            info.RoomInfo.Parent_area_id,
                            info.RoomInfo.Area_id,
                            info.HeartBeatCount,
                            ck.LiveBuvid,
                            timestamp,
                            info.HeartBeatInfo.Timestamp,
                            _securityOptions.UserAgent,
                            info.HeartBeatInfo.Secret_rule,
                            info.HeartBeatInfo.Secret_key,
                            ck.BiliJct,
                            uuid,
                            $"[\"{ck.LiveBuvid}\",\"{uuid}\"]"
                        ),
                        ck.ToString()
                    );
                }

                info.LastBeatTime = Now();

                if (heartBeatResult != null && heartBeatResult.Data != null)
                {
                    info.HeartBeatInfo.Secret_key = heartBeatResult.Data.Secret_key;
                    info.HeartBeatInfo.Secret_rule = heartBeatResult.Data.Secret_rule;
                    info.HeartBeatInfo.Timestamp = heartBeatResult.Data.Timestamp;
                }

                if (heartBeatResult == null || heartBeatResult.Code != 0)
                {
                    logger.LogError("【心跳包】直播间 {room} 发送失败", info.RoomId);
                    logger.LogError(
                        "【原因】{message}",
                        heartBeatResult != null ? heartBeatResult.Message : ""
                    );
                    info.FailedTimes += 1;
                    continue;
                }

                info.HeartBeatCount += 1;
                info.FailedTimes = 0;

                logger.LogInformation(
                    "【直播间】{roomId} 的第 {index} 个心跳包发送成功",
                    info.RoomId,
                    info.HeartBeatCount
                );
            }
        }

        var successCount = infoList.Count(info =>
            info.HeartBeatCount >= _liveFansMedalTaskOptions.HeartBeatNumber
        );
        logger.LogInformation(
            "【直播观看时长】完成情况：{success}/{total} ",
            successCount,
            infoList.Count
        );
    }

    /// <summary>
    /// 点赞直播间
    /// </summary>
    public async Task LikeFansMedalLive(BiliCookie ck)
    {
        if (!await CheckLiveCookie(ck))
            return;

        var infoList = await GetFansMedalInfoList(ck);
        infoList = infoList.FindAll(info => info.LiveRoomInfo.Live_Status != 0);
        logger.LogInformation("当前开播直播间数量：{num}", infoList.Count);
        foreach (var info in infoList)
        {
            // Clike_Time 暂时设置为等于设置的LikeNumber，不清楚是否会被风控，我自己抓包最大值为10
            var request = new LikeLiveRoomRequest(
                info.RoomId,
                ck.BiliJct,
                _liveFansMedalTaskOptions.LikeNumber,
                info.LiveRoomInfo.Uid,
                ck.UserId
            );

            var result = await liveApi.LikeLiveRoom(request.RawTextBuild(), ck.ToString());
            if (result.Code == 0)
            {
                logger.LogInformation("【点赞直播间】{roomId} 完成", info.RoomId);
            }
            else
            {
                logger.LogError("【点赞直播间】{roomId} 时候出现错误", info.RoomId);
                logger.LogError("【原因】{message}", result.Message);
            }

            var delay = new Random().Next(5000, 8000);
            await Task.Delay(delay);
        }
    }

    private async Task<List<FansMedalInfoDto>> GetFansMedalInfoList(BiliCookie ck)
    {
        logger.LogInformation("【获取直播列表】获取拥有粉丝牌的直播列表");
        var medalWallInfo = await liveApi.GetMedalWall(ck.UserId, ck.ToString());

        if (medalWallInfo.Code != 0)
        {
            logger.LogError("【获取直播列表】失败");
            logger.LogError("【原因】{message}", medalWallInfo.Message);
            return new List<FansMedalInfoDto>();
        }

        var infoList = new List<FansMedalInfoDto>();
        foreach (var medal in medalWallInfo.Data.List)
        {
            logger.LogInformation("【主播】{name} ", medal.Target_name);
            if (_liveFansMedalTaskOptions.IsSkipLevel20Medal && medal.Medal_info.Level >= 20)
            {
                logger.LogInformation(
                    "粉丝牌等级为 {level}，观看将不再增长亲密度，跳过",
                    medal.Medal_info.Level
                );
                continue;
            }

            // 通过空间主页信息获取直播间 id
            var liveHostUserId = medal.Medal_info.Target_id;
            var req = new GetSpaceInfoDto() { mid = liveHostUserId };

            var spaceInfo = await upInfoApi.GetSpaceInfo(req, ck.ToString());
            if (spaceInfo.Code != 0)
            {
                logger.LogError("【获取空间信息】失败");
                logger.LogError("【原因】{message}", spaceInfo.Message);
                continue;
            }

            // 用以排除有牌子无直播间的up主
            if (spaceInfo.Data.Live_room is null)
            {
                logger.LogInformation("【主播】{name} 直播间id获取失败，已跳过", medal.Target_name);
                continue;
            }

            var roomId = spaceInfo.Data.Live_room.Roomid;

            // 获取直播间详细信息
            var liveRoomInfo = await liveApi.GetLiveRoomInfo(roomId);
            if (liveRoomInfo.Code != 0)
            {
                logger.LogError("【获取直播间信息】失败");
                logger.LogError("【原因】{message}", liveRoomInfo.Message);
                continue;
            }

            infoList.Add(new FansMedalInfoDto(roomId, medal, liveRoomInfo.Data));
        }

        return infoList;
    }

    /// <summary>
    /// 自动配置直播相关 Cookie，来兼容较低版本中保存的 Cookie 配置
    /// </summary>
    /// <returns>
    /// bool 成功配置 or not
    /// </returns>
    private async Task<bool> CheckLiveCookie(BiliCookie ck)
    {
        // 检测 _biliCookie 是否正确配置
        if (!string.IsNullOrWhiteSpace(ck.LiveBuvid))
            return true;

        try
        {
            logger.LogInformation("检测到直播 Cookie 未正确配置，尝试自动配置中...");

            // 请求主播主页来正确配置 cookie
            var liveHome = await liveApi.GetLiveHome(ck.ToString());
            var liveHomeContent = JsonConvert.DeserializeObject<BiliApiResponse>(
                await liveHome.Content.ReadAsStringAsync()
            );
            if (liveHomeContent.Code != 0)
            {
                throw new Exception(liveHomeContent.Message);
            }

            var setHeader = liveHome.Headers.FirstOrDefault(header => header.Key == "Set-Cookie");
            ck.MergeCurrentCookie(setHeader.Value.ToList());

            logger.LogDebug("LiveBuvid {value}", ck.LiveBuvid);
            logger.LogInformation("直播 Cookie 配置成功！");
        }
        catch (Exception exception)
        {
            logger.LogError("【配置直播Cookie】失败，放弃执行后续任务...");
            logger.LogError("【原因】{message}", exception.Message);
            return false;
        }

        return true;
    }
}
