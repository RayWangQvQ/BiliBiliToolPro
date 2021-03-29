using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 漫画
    /// </summary>
    public class MangaDomainService : IMangaDomainService
    {
        private readonly ILogger<MangaDomainService> _logger;
        private readonly IMangaApi _mangaApi;
        private readonly DailyTaskOptions _dailyTaskOptions;

        public MangaDomainService(ILogger<MangaDomainService> logger,
            IMangaApi mangaApi,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions)
        {
            _logger = logger;
            _mangaApi = mangaApi;
            _dailyTaskOptions = dailyTaskOptions.CurrentValue;
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        public void MangaSign()
        {
            BiliApiResponse response;
            try
            {
                response = _mangaApi.ClockIn(_dailyTaskOptions.DevicePlatform).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                //ignore
                //重复签到会报400异常,这里忽略掉
                _logger.LogInformation("【签到结果】失败");
                _logger.LogInformation("【原因】今日已签到过，无法重复签到");
                return;
            }

            if (response.Code == 0)
            {
                _logger.LogInformation("【签到结果】成功");
            }
            else
            {
                _logger.LogInformation("【签到结果】失败");
                _logger.LogInformation("【原因】{msg}", response.Message);
            }
        }

        /// <summary>
        /// 获取大会员漫画权益
        /// </summary>
        /// <param name="reason_id">权益号，由https://api.bilibili.com/x/vip/privilege/my得到权益号数组，取值范围为数组中的整数
        /// 这里为方便直接取1，为领取漫读劵，暂时不取其他的值</param>
        public void ReceiveMangaVipReward(int reason_id, UserInfo userInfo)
        {
            if (userInfo.GetVipType() == 0)
            {
                _logger.LogInformation("不是会员，跳过");
                return;
            }

            int day = DateTime.Today.Day;

            if (day != _dailyTaskOptions.DayOfReceiveVipPrivilege)
            {
                //一个月执行一次就行
                _logger.LogInformation("【目标日期】{target}号", _dailyTaskOptions.DayOfReceiveVipPrivilege);
                _logger.LogInformation("【今天】{day}号", day);
                _logger.LogInformation("跳过");
                return;
            }

            var response = _mangaApi.ReceiveMangaVipReward(reason_id)
                .GetAwaiter().GetResult();
            if (response.Code == 0)
            {
                _logger.LogInformation("【领取结果】成功");
                _logger.LogInformation($"【获取】{response.Data.Amount}张漫读劵");
            }
            else
            {
                _logger.LogInformation("【领取结果】失败");
                _logger.LogInformation("【原因】{msg}", response.Message);
            }
        }
    }
}
