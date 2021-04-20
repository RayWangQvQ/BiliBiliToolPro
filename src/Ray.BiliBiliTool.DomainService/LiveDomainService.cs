using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Relation;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

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
        private readonly LiveLotteryTaskOptions _liveLotteryTaskOptions;
        private readonly BiliCookie _biliCookie;
        private readonly DailyTaskOptions _dailyTaskOptions;

        public LiveDomainService(ILogger<LiveDomainService> logger,
            ILiveApi liveApi,
            IRelationApi relationApi,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IOptionsMonitor<LiveLotteryTaskOptions> liveLotteryTaskOptions,
            BiliCookie biliCookie)
        {
            _logger = logger;
            _liveApi = liveApi;
            _relationApi = relationApi;
            _liveLotteryTaskOptions = liveLotteryTaskOptions.CurrentValue;
            _biliCookie = biliCookie;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
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

            var response = _liveApi.ExchangeSilver2Coin().GetAwaiter().GetResult();
            if (response.Code == 0)
            {
                result = true;
                _logger.LogInformation("【兑换结果】成功");
            }
            else
            {
                _logger.LogInformation("【兑换结果】失败");
                _logger.LogInformation("【原因】{0}", response.Message);
            }

            var queryStatus = _liveApi.GetExchangeSilverStatus().GetAwaiter().GetResult();
            _logger.LogInformation("【银瓜子余额】 {0}", queryStatus.Data.Silver);

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
    }
}
