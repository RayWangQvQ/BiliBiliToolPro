using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
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
        private readonly IOptionsMonitor<DailyTaskOptions> _dailyTaskOptions;

        public MangaDomainService(ILogger<MangaDomainService> logger,
            IMangaApi mangaApi,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions)
        {
            _logger = logger;
            _mangaApi = mangaApi;
            _dailyTaskOptions = dailyTaskOptions;
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        public void MangaSign()
        {
            BiliApiResponse response;
            try
            {
                response = _mangaApi.ClockIn(_dailyTaskOptions.CurrentValue.DevicePlatform).Result;
            }
            catch (Exception)
            {
                //ignore
                //重复签到会报400异常,这里忽略掉
                _logger.LogInformation("哔哩哔哩漫画已经签到过了");
                return;
            }

            if (response.Code == 0)
            {
                _logger.LogInformation("完成漫画签到");
            }
            else
            {
                _logger.LogInformation("漫画签到异常");
            }
        }

        /// <summary>
        /// 获取大会员漫画权益
        /// </summary>
        /// <param name="reason_id">权益号，由https://api.bilibili.com/x/vip/privilege/my得到权益号数组，取值范围为数组中的整数
        /// 这里为方便直接取1，为领取漫读劵，暂时不取其他的值</param>
        public void ReceiveMangaVipReward(int reason_id, UseInfo userIfo)
        {
            int day = DateTime.Today.Day;

            if (day != 1 || userIfo.GetVipType() == 0)
            {
                //一个月执行一次就行
                _logger.LogInformation("今天是{day}号，每月的1号会自动为您领取漫读券", day);
                return;
            }

            var response = _mangaApi.ReceiveMangaVipReward(reason_id).Result;
            if (response.Code == 0)
            {
                _logger.LogInformation($"大会员成功领取{response.Data.Amount}张漫读劵");
            }
            else
            {
                _logger.LogInformation($"大会员领取漫读劵失败，原因为:{response.Message}");
            }
        }
    }
}
