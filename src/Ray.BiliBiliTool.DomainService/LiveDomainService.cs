using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
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
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 直播
    /// </summary>
    public class LiveDomainService : ILiveDomainService
    {
        private readonly ILogger<LiveDomainService> _logger;
        private readonly ILiveApi _liveApi;
        private readonly IRelationApi _relationApi;
        private readonly ILiveTraceApi _liveTraceApi;
        private readonly IUserInfoApi _userInfoApi;
        private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions;
        private readonly LiveFansMedalTaskOptions _liveFansMedalTaskOptions;
        private readonly DailyTaskOptions _dailyTaskOptions;
        private readonly SecurityOptions _securityOptions;
        private readonly BiliCookie _biliCookie;


        public LiveDomainService(ILogger<LiveDomainService> logger,
            ILiveApi liveApi,
            IRelationApi relationApi,
            ILiveTraceApi liveTraceApi,
            IUserInfoApi userInfoApi,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
            IOptionsMonitor<LiveFansMedalTaskOptions> liveFansMedalTaskOptions,
            IOptionsMonitor<SecurityOptions> securityOptions,
            BiliCookie biliCookie)
        {
            _logger = logger;
            _liveApi = liveApi;
            _relationApi = relationApi;
            _liveTraceApi = liveTraceApi;
            _userInfoApi = userInfoApi;
            _liveLotteryTaskOptions = liveLotteryTaskOptions.CurrentValue;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
            _liveFansMedalTaskOptions = liveFansMedalTaskOptions.CurrentValue;
            _securityOptions = securityOptions.CurrentValue;
            _biliCookie = biliCookie;

        }

        /// <summary>
        /// 本次通过天选关注的主播
        /// </summary>
        private List<ListItemDto> _tianXuanFollowed = new List<ListItemDto>();

        /// <summary>
        /// 开始抽奖前最后一个关注的up
        /// </summary>
        private long _lastFollowUpId;

        /// <summary>
        /// 直播签到
        /// </summary>
        public void LiveSign()
        {
            var response = _liveApi.Sign()
                .GetAwaiter().GetResult();

            if (response.Code == 0)
            {
                _logger.LogInformation("【签到结果】成功");
                _logger.LogInformation("【本次获取】{text},{special}", response.Data.Text, response.Data.SpecialText);
            }
            else
            {
                _logger.LogInformation("【签到结果】失败");
                _logger.LogInformation("【原因】{msg}", response.Message);
            }
        }

        /// <summary>
        /// 直播中心银瓜子兑换B币
        /// </summary>
        /// <returns>兑换银瓜子后硬币余额</returns>
        public bool ExchangeSilver2Coin()
        {
            var result = false;

            if (_dailyTaskOptions.DayOfExchangeSilver2Coin == 0)
            {
                _logger.LogInformation("已配置为关闭，跳过");
                return false;
            }

            int targetDay = _dailyTaskOptions.DayOfExchangeSilver2Coin == -2
                ? DateTime.Today.Day
                : _dailyTaskOptions.DayOfExchangeSilver2Coin == -1
                    ? DateTime.Today.LastDayOfMonth().Day
                    : _dailyTaskOptions.DayOfExchangeSilver2Coin;

            _logger.LogInformation("【目标兑换日期】{targetDay}号", targetDay);
            _logger.LogInformation("【今天】{day}号", DateTime.Today.Day);

            if (DateTime.Today.Day != targetDay)
            {
                _logger.LogInformation("跳过");
                return false;
            }

            BiliApiResponse<LiveWalletStatusResponse> queryStatus = _liveApi.GetLiveWalletStatus().GetAwaiter().GetResult();
            _logger.LogInformation("【银瓜子余额】 {silver}", queryStatus.Data.Silver);
            _logger.LogInformation("【硬币余额】 {coin}", queryStatus.Data.Coin);
            _logger.LogInformation("【今日剩余兑换次数】 {left}", queryStatus.Data.Silver_2_coin_left);

            if (queryStatus.Data.Silver_2_coin_left <= 0) return false;

            _logger.LogInformation("开始尝试兑换...");
            Silver2CoinRequest request = new(_biliCookie.BiliJct);
            var response = _liveApi.Silver2Coin(request).GetAwaiter().GetResult();
            if (response.Code == 0)
            {
                result = true;
                _logger.LogInformation("【兑换结果】成功兑换 {coin} 枚硬币", response.Data.Coin);
                _logger.LogInformation("【银瓜子余额】 {silver}", response.Data.Silver);
            }
            else
            {
                _logger.LogInformation("【兑换结果】失败");
                _logger.LogInformation("【原因】{reason}", response.Message);
            }

            return result;
        }

        #region 天选时刻抽奖
        /// <summary>
        /// 天选抽奖
        /// </summary>
        public void TianXuan()
        {
            _tianXuanFollowed = new List<ListItemDto>();

            if (_liveLotteryTaskOptions.AutoGroupFollowings)
            {
                //获取此时最后一个关注的up，此后再新增的关注，与参与成功的抽奖，取交集，就是本地新增的天选关注
                _lastFollowUpId = GetLastFollowUpId();
            }

            //获取直播的分区
            List<AreaDto> areaList = _liveApi.GetAreaList()
                .GetAwaiter().GetResult()
                .Data.Data;

            //遍历分区
            int count = 0;
            foreach (var area in areaList)
            {
                _logger.LogInformation("【扫描分区】{area}..." + Environment.NewLine, area.Name);

                string defaultSort = "";
                //每个分区下搜索5页
                for (int i = 1; i < 6; i++)
                {
                    var reData = _liveApi.GetList(area.Id, i, sortType: defaultSort)
                        .GetAwaiter().GetResult()
                        .Data;
                    foreach (var item in reData.List ?? new List<ListItemDto>())
                    {
                        if (item.Pendant_info == null || item.Pendant_info.Count == 0) continue;
                        var suc = item.Pendant_info.TryGetValue("2", out var pendant);
                        if (!suc) continue;
                        if (pendant.Pendent_id != 504) continue;
                        count++;

                        TryJoinTianXuan(item);
                    }
                    if (reData.Has_more != 1) break;
                    defaultSort = reData.New_tags.FirstOrDefault()?.Sort_type ?? "";
                }
                defaultSort = "";
            }

            if (count == 0)
            {
                _logger.LogInformation("未搜索到直播间");
                return;
            }
        }

        public void TryJoinTianXuan(ListItemDto target)
        {
            _logger.LogDebug("【房间】{name}", target.Title);
            try
            {
                //黑名单
                if (_liveLotteryTaskOptions.DenyUidList.Contains(target.Uid.ToString()))
                {
                    _logger.LogDebug("黑名单，跳过");
                    return;
                }

                CheckTianXuanDto check = _liveApi.CheckTianXuan(target.Roomid)
                    .GetAwaiter().GetResult()
                    .Data;

                if (check == null)
                {
                    _logger.LogDebug("数据异常，跳过");
                    return;
                }

                if (check.Status != TianXuanStatus.Enable)
                {
                    _logger.LogDebug("已开奖，跳过" + Environment.NewLine);
                    return;
                }

                //根据配置过滤
                if (!check.AwardNameIsSatisfied(_liveLotteryTaskOptions.IncludeAwardNameList, _liveLotteryTaskOptions.ExcludeAwardNameList))
                {
                    _logger.LogDebug("不满足配置的筛选条件，跳过" + Environment.NewLine);
                    return;
                }

                //是否需要赠礼
                if (check.Gift_price > 0)
                {
                    _logger.LogDebug("【赠礼】{gift}", check.GiftDesc);
                    _logger.LogDebug("需赠送礼物，跳过" + Environment.NewLine);
                    return;
                }

                //条件
                if (check.Require_type != RequireType.None && check.Require_type != RequireType.Follow)
                {
                    _logger.LogDebug("【条件】{text}", check.Require_text);
                    _logger.LogDebug("要求粉丝勋章，跳过");
                    return;
                }

                _logger.LogInformation("【房间】{name}", target.ShortTitle);
                _logger.LogInformation("【主播】{name}({id})", target.Uname, target.Uid);
                _logger.LogInformation("【奖品】{name}【条件】{text}", check.Award_name, check.Require_text);

                var request = new JoinTianXuanRequest
                {
                    Id = check.Id,
                    Gift_id = check.Gift_id,
                    Gift_num = check.Gift_num,
                    Csrf = _biliCookie.BiliJct
                };
                var re = _liveApi.Join(request)
                    .GetAwaiter().GetResult();
                if (re.Code == 0)
                {
                    _logger.LogInformation("【抽奖】成功 √" + Environment.NewLine);
                    if (check.Require_type == RequireType.Follow)
                        _tianXuanFollowed.AddIfNotExist(target, x => x.Uid == target.Uid);
                    return;
                }

                _logger.LogInformation("【抽奖】失败");
                _logger.LogInformation("【原因】{msg}" + Environment.NewLine, re.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("【异常】{msg}，{detail}" + Environment.NewLine, ex.Message, ex);
                //ignore
            }
        }

        /// <summary>
        /// 将本次抽奖新增的关注统一转移到指定分组中
        /// </summary>
        public void GroupFollowing()
        {
            if (!_tianXuanFollowed.Any())
            {
                _logger.LogInformation("未关注主播");
                return;
            }
            _logger.LogInformation("【抽奖的主播】{ups}",
                string.Join("，", _tianXuanFollowed.Select(x => x.Uname)));

            //目标分组up集合
            List<ListItemDto> targetUps = GetNeedGroup();
            _logger.LogInformation("【将自动分组】{ups}",
                string.Join("，", targetUps.Select(x => x.Uname)));

            if (!targetUps.Any())
            {
                return;
            }

            //目标分组Id
            long targetGroupId = GetOrCreateTianXuanGroupId();

            //执行批量分组
            var referer = string.Format(RelationApiConstant.CopyReferer, _biliCookie.UserId);
            var req = new CopyUserToGroupRequest(
                targetUps.Select(x => x.Uid).ToList(),
                targetGroupId.ToString(),
                _biliCookie.BiliJct);
            var re = _relationApi.CopyUpsToGroup(req, referer)
                .GetAwaiter().GetResult();

            if (re.Code == 0)
            {
                _logger.LogInformation("【分组结果】全部成功");
            }
            else
            {
                _logger.LogWarning("【分组结果】失败");
                _logger.LogWarning("【原因】{msg}", re.Message);
            }
        }



        /// <summary>
        /// 获取抽奖前最后一个关注的up
        /// </summary>
        /// <returns></returns>
        private long GetLastFollowUpId()
        {
            var followings = _relationApi
                .GetFollowings(new GetFollowingsRequest(long.Parse(_biliCookie.UserId), FollowingsOrderType.TimeDesc))
                .GetAwaiter().GetResult();
            return followings.Data.List.FirstOrDefault()?.Mid ?? 0;
        }

        /// <summary>
        /// 获取本次需要自动分组的主播
        /// </summary>
        /// <returns></returns>
        private List<ListItemDto> GetNeedGroup()
        {
            List<long> addUpIds = new List<long>();

            //获取最后一个upId之后关注的所有upId
            var followings = _relationApi
                .GetFollowings(new GetFollowingsRequest(long.Parse(_biliCookie.UserId), FollowingsOrderType.TimeDesc))
                .GetAwaiter().GetResult();

            foreach (UpInfo item in followings.Data.List)
            {
                if (item.Mid == _lastFollowUpId)
                {
                    break;
                }

                addUpIds.Add(item.Mid);
            }

            //和成功抽奖的主播取交集
            List<ListItemDto> target = new List<ListItemDto>();
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
        private long GetOrCreateTianXuanGroupId()
        {
            //获取天选分组Id，没有就创建
            long groupId = 0;
            string referer = string.Format(RelationApiConstant.GetTagsReferer, _biliCookie.UserId);
            var groups = _relationApi.GetTags(referer).GetAwaiter().GetResult();
            var tianXuanGroup = groups.Data.FirstOrDefault(x => x.Name == "天选时刻");
            if (tianXuanGroup == null)
            {
                _logger.LogInformation("“天选时刻”分组不存在，尝试创建...");
                //创建一个
                var createRe = _relationApi.CreateTag(new CreateTagRequest { Tag = "天选时刻", Csrf = _biliCookie.BiliJct })
                    .GetAwaiter().GetResult();
                groupId = createRe.Data.Tagid;
                _logger.LogInformation("创建成功");
            }
            else
            {
                _logger.LogInformation("“天选时刻”分组已存在");
                groupId = tianXuanGroup.Tagid;
            }

            return groupId;
        }
        #endregion

        public void SendDanmakuToFansMedalLive()
        {
            if (!CheckLiveCookie()) return;

            GetFansMedalInfoList().ForEach(info =>
            {
                var medal = info.MedalInfo;

                _logger.LogInformation("【直播间】{liveRoomName}", medal.Target_name);
                _logger.LogInformation("【粉丝牌】{medalName}", medal.Medal_info.Medal_name);

                _logger.LogInformation("正在发送弹幕...");

                // 通过空间主页信息获取直播间 id
                int liveHostUserId = medal.Medal_info.Target_id;
                var spaceInfo = _userInfoApi.GetSpaceInfo(liveHostUserId).Result;
                if (spaceInfo.Code != 0)
                {
                    _logger.LogError("【获取直播间信息】失败");
                    _logger.LogError("【原因】{message}", spaceInfo.Message);
                    return;
                }

                // 发送弹幕
                var sendResult = _liveApi.SendLiveDanmuku(new SendLiveDanmukuRequest(
                    _biliCookie.BiliJct,
                    spaceInfo.Data.Live_room.Roomid,
                    _liveFansMedalTaskOptions.DanmakuContent)).Result;

                if (sendResult.Code != 0)
                {
                    _logger.LogError("【弹幕发送】失败");
                    _logger.LogError("【原因】{message}", sendResult.Message);
                    return;
                }

                _logger.LogInformation("【弹幕发送】成功~，你和主播 {name} 的亲密值增加了100！", spaceInfo.Data.Name);
            });
        }

        public void SendHeartBeatToFansMedalLive()
        {
            if (!CheckLiveCookie()) return;

            var infoList = new List<HeartBeatIterationInfoDto>();
            GetFansMedalInfoList().ForEach((medal) => infoList.Add(new(medal.RoomId, medal.LiveRoomInfo, new(), 0, 0)));

            if (infoList.Count == 0)
            {
                _logger.LogInformation("【直播观看时长】跳过，未检测到符合条件的主播");
                return;
            }

            var Now = () => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            while (infoList.Min(
                       info => info.FailedTimes >= _liveFansMedalTaskOptions.HeartBeatSendGiveUpThreshold
                           ? int.MaxValue :
                           info.HeartBeatCount)
                   < _liveFansMedalTaskOptions.HeartBeatNumber)
            {
                foreach (var info in infoList)
                {
                    // 忽略连续失败超过上限的直播间
                    if (info.FailedTimes >= _liveFansMedalTaskOptions.HeartBeatSendGiveUpThreshold) continue;

                    string uuid = Guid.NewGuid().ToString();
                    var current = Now();
                    if (current - info.LastBeatTime <= (LiveFansMedalTaskOptions.HeartBeatInterval + 5) * 1000)
                    {
                        int sleepTime = (int)((LiveFansMedalTaskOptions.HeartBeatInterval + 5) * 1000 - (current - info.LastBeatTime));
                        _logger.LogDebug("【休眠】{time} 毫秒", sleepTime);
                        Thread.Sleep(sleepTime);
                    }

                    // Heart Beat 接口
                    var timestamp = Now();
                    BiliApiResponse<HeartBeatResponse> heartBeatResult = null;
                    if (info.HeartBeatCount == 0)
                    {
                        heartBeatResult = _liveTraceApi.EnterRoom(
                            new EnterRoomRequest(
                                info.RoomId,
                                info.RoomInfo.Parent_area_id,
                                info.RoomInfo.Area_id,
                                info.HeartBeatCount,
                                timestamp,
                                _securityOptions.UserAgent,
                                _biliCookie.BiliJct,
                                info.RoomInfo.Uid),
                            $"[\"{_biliCookie.LiveBuvid}\",\"{uuid}\"]")
                            .Result;
                    }
                    else
                    {
                        heartBeatResult = _liveTraceApi.HeartBeat(
                            new HeartBeatRequest(
                                info.RoomId,
                                info.RoomInfo.Parent_area_id,
                                info.RoomInfo.Area_id,
                                info.HeartBeatCount,
                                _biliCookie.LiveBuvid,
                                timestamp,
                                info.HeartBeatInfo.Timestamp,
                                _securityOptions.UserAgent,
                                info.HeartBeatInfo.Secret_rule,
                                info.HeartBeatInfo.Secret_key,
                                _biliCookie.BiliJct,
                                uuid),
                            $"[\"{_biliCookie.LiveBuvid}\",\"{uuid}\"]")
                            .Result;
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
                        _logger.LogError("【心跳包】直播间 {room} 发送失败", info.RoomId);
                        _logger.LogError("【原因】{message}", heartBeatResult != null ? heartBeatResult.Message : "");
                        info.FailedTimes += 1;
                        continue;
                    }

                    info.HeartBeatCount += 1;
                    info.FailedTimes = 0;

                    _logger.LogInformation("【直播间】{roomId} 的第 {index} 个心跳包发送成功", info.RoomId, info.HeartBeatCount);
                }
            }

            var successCount = infoList.Count(info => info.HeartBeatCount >= _liveFansMedalTaskOptions.HeartBeatNumber);
            _logger.LogInformation("【直播观看时长】完成情况：{success}/{total} ", successCount, infoList.Count);
        }

        public void LikeFansMedalLive()
        {
            if (!CheckLiveCookie()) return;

            GetFansMedalInfoList().ForEach(info =>
            {
                var result = _liveApi.LikeLiveRoom(new LikeLiveRoomRequest(info.RoomId, _biliCookie.BiliJct)).Result;
                if (result.Code == 0)
                {
                    _logger.LogInformation("【点赞直播间】{roomId} 完成", info.RoomId);
                }
                else
                {
                    _logger.LogError("【点赞直播间】{roomId} 时候出现错误", info.RoomId);
                    _logger.LogError("【原因】{message}", result.Message);
                }
            });
        }

        private List<FansMedalInfoDto> GetFansMedalInfoList()
        {
            _logger.LogInformation("【获取直播列表】获取拥有粉丝牌的直播列表");
            var medalWallInfo = this._liveApi.GetMedalWall(int.Parse(this._biliCookie.UserId)).Result;

            if (medalWallInfo.Code != 0)
            {
                _logger.LogError("【获取直播列表】失败");
                _logger.LogError("【原因】{message}", medalWallInfo.Message);
                return null;
            }

            var infoList = new List<FansMedalInfoDto>();
            foreach (var medal in medalWallInfo.Data.List)
            {
                _logger.LogInformation("【主播】{name} ", medal.Target_name);
                if (_liveFansMedalTaskOptions.IsSkipLevel20Medal && medal.Medal_info.Level >= 20)
                {
                    _logger.LogInformation("粉丝牌等级为 {level}，观看将不再增长亲密度，跳过", medal.Medal_info.Level);
                    continue;
                }

                // 通过空间主页信息获取直播间 id
                int liveHostUserId = medal.Medal_info.Target_id;
                var spaceInfo = _userInfoApi.GetSpaceInfo(liveHostUserId).Result;
                if (spaceInfo.Code != 0)
                {
                    _logger.LogError("【获取空间信息】失败");
                    _logger.LogError("【原因】{message}", spaceInfo.Message);
                    continue;
                }

                var roomId = spaceInfo.Data.Live_room.Roomid;

                // 获取直播间详细信息
                var liveRoomInfo = _liveApi.GetLiveRoomInfo(roomId).Result;
                if (liveRoomInfo.Code != 0)
                {
                    _logger.LogError("【获取直播间信息】失败");
                    _logger.LogError("【原因】{message}", liveRoomInfo.Message);
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
        private bool CheckLiveCookie()
        {
            // 检测 _biliCookie 是否正确配置
            if (string.IsNullOrWhiteSpace(_biliCookie.LiveBuvid))
            {
                try
                {
                    _logger.LogInformation("检测到直播 Cookie 未正确配置，尝试自动配置中...");

                    // 请求主播主页来正确配置 cookie
                    var liveHome = _liveApi.GetLiveHome().Result;
                    var liveHomeContent = JsonConvert.DeserializeObject<BiliApiResponse>(liveHome.Content.ReadAsStringAsync().Result);
                    if (liveHomeContent.Code != 0)
                    {
                        throw new Exception(liveHomeContent.Message);
                    }

                    IEnumerable<string> liveCookies = liveHome.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                    var ckItemList = new List<string>();
                    foreach (var item in liveCookies)
                    {
                        ckItemList.Add(item.Split(';').FirstOrDefault());
                    }
                    _biliCookie.LiveBuvid = CookieInfo.BuildCookieItemDictionaryByCookieItemList(
                        ckItemList,
                        null,
                        v => v.Contains(',') ? Uri.EscapeDataString(v) : v)
                        [_biliCookie.GetType().GetPropertyDescription(nameof(BiliCookie.LiveBuvid))];

                    _logger.LogDebug("LiveBuvid {value}", _biliCookie.LiveBuvid);
                    _logger.LogInformation("直播 Cookie 配置成功！");
                }
                catch (Exception exception)
                {
                    _logger.LogError("【配置直播Cookie】失败，放弃执行后续任务...");
                    _logger.LogError("【原因】{message}", exception.Message);
                    return false;
                }
            }
            return true;
        }
    }
}
