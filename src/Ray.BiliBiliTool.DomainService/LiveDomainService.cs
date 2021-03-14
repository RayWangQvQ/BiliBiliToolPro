using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;
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
        private readonly BiliCookie _biliCookie;
        private readonly DailyTaskOptions _dailyTaskOptions;

        private List<ListItemDto> _targetTianXuanList = new List<ListItemDto>();

        public LiveDomainService(ILogger<LiveDomainService> logger,
            ILiveApi liveApi,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            BiliCookie biliCookie)
        {
            _logger = logger;
            _liveApi = liveApi;
            _biliCookie = biliCookie;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
        }

        /// <summary>
        /// 直播签到
        /// </summary>
        public void LiveSign()
        {
            var response = _liveApi.Sign()
                .GetAwaiter().GetResult();

            if (response.Code == 0)
            {
                _logger.LogInformation("直播签到成功，本次签到获得{text},{special}", response.Data.Text, response.Data.SpecialText);
            }
            else
            {
                _logger.LogInformation(response.Message);
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
                _logger.LogInformation("已配置为不进行兑换，跳过兑换任务");
                return false;
            }

            int targetDay = _dailyTaskOptions.DayOfExchangeSilver2Coin == -2
                ? DateTime.Today.Day
                : _dailyTaskOptions.DayOfExchangeSilver2Coin == -1
                    ? DateTime.Today.LastDayOfMonth().Day
                    : _dailyTaskOptions.DayOfExchangeSilver2Coin;

            if (DateTime.Today.Day != targetDay)
            {
                _logger.LogInformation("目标兑换日期为{targetDay}号，今天是{day}号，跳过兑换任务", targetDay, DateTime.Today.Day);
                return false;
            }

            var response = _liveApi.ExchangeSilver2Coin().GetAwaiter().GetResult();
            if (response.Code == 0)
            {
                result = true;
                _logger.LogInformation("银瓜子兑换硬币成功");
            }
            else
            {
                _logger.LogInformation("银瓜子兑换硬币失败，原因：{0}", response.Message);
            }

            var queryStatus = _liveApi.GetExchangeSilverStatus().GetAwaiter().GetResult();
            _logger.LogInformation("当前银瓜子余额: {0}", queryStatus.Data.Silver);

            return result;
        }

        public void TianXuan()
        {
            //获取直播的分区
            List<AreaDto> areaList = _liveApi.GetAreaList()
                .GetAwaiter().GetResult()
                .Data.Data;

            //遍历分区
            int count = 0;
            foreach (var area in areaList)
            {
                _logger.LogInformation("正在扫描分区：{area}...\r\n", area.Name);

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

                        _targetTianXuanList.Add(item);
                        count++;

                        TryJoinTianXuan(item);
                    }
                    if (reData.Has_more != 1) break;
                    defaultSort = reData.New_tags.FirstOrDefault()?.Sort_type ?? "";
                }
                defaultSort = "";
            }
            if (count == 0) _logger.LogInformation("未搜索到直播间");
        }

        public void TryJoinTianXuan(ListItemDto target)
        {
            _logger.LogInformation("开始检查直播间：{name}({id})", target.Title, target.Roomid);

            try
            {
                CheckTianXuanDto check = _liveApi.CheckTianXuan(target.Roomid)
                    .GetAwaiter().GetResult()
                    .Data;

                if (check == null)
                {
                    _logger.LogInformation("数据异常，跳过");
                    return;
                }

                _logger.LogInformation("奖励：{name}，条件：{text}", check.Award_name, check.Require_text);

                if (check.Status != 1)
                {
                    _logger.LogInformation("已开奖，跳过\r\n");
                    return;
                }

                if (check.Gift_price != 0)
                {
                    _logger.LogInformation("需要赠送礼物才能参与，跳过\r\n");
                    return;
                }

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
                    _logger.LogInformation("参与抽奖成功!\r\n");
                    return;
                }

                _logger.LogInformation("参与抽奖失败，原因：{msg}\r\n", re.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("发生异常：{msg}，{detail}\r\n", ex.Message, ex);
                //ignore
            }
        }
    }
}
