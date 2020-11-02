using System;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.Application
{
    public class DailyTaskAppService : IDailyTaskAppService
    {
        private readonly ILogger<DailyTaskAppService> _logger;
        private readonly IAccountDomainService _loginDomainService;
        private readonly IVideoDomainService _videoDomainService;
        private readonly IMangaDomainService _mangaDomainService;
        private readonly ILiveDomainService _liveDomainService;
        private readonly IVipPrivilegeDomainService _vipPrivilegeDomainService;
        private readonly IChargeDomainService _chargeDomainService;

        public DailyTaskAppService(
            ILogger<DailyTaskAppService> logger,
            IAccountDomainService loginDomainService,
            IVideoDomainService videoDomainService,
            IMangaDomainService mangaDomainService,
            ILiveDomainService liveDomainService,
            IVipPrivilegeDomainService vipPrivilegeDomainService,
            IChargeDomainService chargeDomainService)
        {
            _logger = logger;
            _loginDomainService = loginDomainService;
            _videoDomainService = videoDomainService;
            _mangaDomainService = mangaDomainService;
            _liveDomainService = liveDomainService;
            _vipPrivilegeDomainService = vipPrivilegeDomainService;
            _chargeDomainService = chargeDomainService;
        }

        public void DoDailyTask()
        {
            UseInfo userInfo;
            DailyTaskInfo dailyTaskInfo;

            userInfo = Login();
            dailyTaskInfo = GetDailyTaskStatus();

            WatchAndShareVideo(dailyTaskInfo);
            AddCoinsForVideo();
            LiveSign();
            userInfo.Money = ExchangeSilver2Coin();
            ReceiveVipPrivilege(userInfo);
            Charge(userInfo);
            MangaSign();
            ReceiveMangaVipReward(userInfo);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        private UseInfo Login()
        {
            UseInfo userInfo = null;
            try
            {
                userInfo = _loginDomainService.LoginByCookie();
            }
            catch (Exception e)
            {
                _logger.LogCritical("登录失败，任务结束。Msg:{msg}\r\n", e.Message);
            }
            if (userInfo == null) throw new Exception("登录失败，请检查Cookie");//终止流程
            return userInfo;
        }

        /// <summary>
        /// 获取任务完成情况
        /// </summary>
        /// <returns></returns>
        private DailyTaskInfo GetDailyTaskStatus()
        {
            var result = new DailyTaskInfo();

            try
            {
                result = _loginDomainService.GetDailyTaskStatus();
            }
            catch (Exception e)
            {
                _logger.LogCritical("获取任务完成情况失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }

            return result;
        }

        /// <summary>
        /// 观看、分享视频
        /// </summary>
        private void WatchAndShareVideo(DailyTaskInfo dailyTaskInfo)
        {
            try
            {
                _videoDomainService.WatchAndShareVideo(dailyTaskInfo);
            }
            catch (Exception e)
            {
                _logger.LogCritical("观看、分享视频失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 投币任务
        /// </summary>
        private void AddCoinsForVideo()
        {
            try
            {
                _videoDomainService.AddCoinsForVideo();
            }
            catch (Exception e)
            {
                _logger.LogCritical("投币失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 直播中心签到
        /// </summary>
        private void LiveSign()
        {
            try
            {
                _liveDomainService.LiveSign();
            }
            catch (Exception e)
            {
                _logger.LogCritical("直播中心签到失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 直播中心的银瓜子兑换硬币
        /// </summary>
        private long ExchangeSilver2Coin()
        {
            long result = 0;
            try
            {
                result = _liveDomainService.ExchangeSilver2Coin();
            }
            catch (Exception e)
            {
                _logger.LogCritical("直播中心的银瓜子兑换硬币失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }

            return result;
        }

        /// <summary>
        /// 月初领取大会员福利
        /// </summary>
        private void ReceiveVipPrivilege(UseInfo userInfo)
        {
            try
            {
                _vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo);
            }
            catch (Exception e)
            {
                _logger.LogCritical("领取大会员福利失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 月底充电
        /// </summary>
        private void Charge(UseInfo userInfo)
        {
            try
            {
                _chargeDomainService.Charge(userInfo);
            }
            catch (Exception e)
            {
                _logger.LogCritical("充电失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 漫画签到
        /// </summary>
        private void MangaSign()
        {
            try
            {
                _mangaDomainService.MangaSign();
            }
            catch (Exception e)
            {
                _logger.LogCritical("漫画签到失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }
        }

        /// <summary>
        /// 获取每月大会员漫画权益
        /// </summary>
        private void ReceiveMangaVipReward(UseInfo userInfo)
        {
            try
            {
                _mangaDomainService.ReceiveMangaVipReward(1, userInfo);
            }
            catch (Exception e)
            {
                _logger.LogCritical("领取大会员漫画权益失败，继续其他任务。Msg:{msg}\r\n", e.Message);
            }

        }
    }
}
