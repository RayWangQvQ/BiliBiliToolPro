using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    public class LiveDomainService : ILiveDomainService
    {
        private readonly ILogger<LiveDomainService> _logger;
        private readonly ILiveApi _liveApi;
        private readonly ICoinDomainService _coinDomainService;

        public LiveDomainService(ILogger<LiveDomainService> logger,
            ILiveApi liveApi,
            ICoinDomainService coinDomainService)
        {
            _logger = logger;
            _liveApi = liveApi;
            _coinDomainService = coinDomainService;
        }

        /// <summary>
        /// 直播签到
        /// </summary>
        public void LiveSign()
        {
            _logger.LogInformation("开始直播签到");

            var response = _liveApi.Sign().Result;

            if (response.Code == 0)
            {
                _logger.LogInformation($"直播签到成功，本次签到获得{response.Data.Text},{response.Data.SpecialText}");
                //desp.appendDesp("直播签到成功，本次签到获得" + data.get("text").getAsstring() + "," + data.get("specialText").getAsstring());
            }
            else
            {
                _logger.LogDebug(response.Message);
            }
        }

        /// <summary>
        /// 直播中心银瓜子兑换B币
        /// </summary>
        /// <returns>兑换银瓜子后硬币余额</returns>
        public int ExchangeSilver2Coin()
        {
            var response = _liveApi.ExchangeSilver2Coin().Result;
            if (response.Code == 0)
            {
                _logger.LogInformation("银瓜子兑换硬币成功");
                //desp.appendDesp("银瓜子兑换硬币成功");
            }
            else
            {
                _logger.LogInformation("银瓜子兑换硬币失败，原因：{0}", response.Message);
                //desp.appendDesp("银瓜子兑换硬币失败 原因是: " + resultJson.get("msg").getAsstring());
            }

            var queryStatus = _liveApi.GetExchangeSilverStatus().Result;
            int silver2coinMoney = _coinDomainService.GetCoinBalance();

            _logger.LogInformation("当前银瓜子余额: {0}", queryStatus.Data.Silver);
            //desp.appendDesp("当前银瓜子余额: " + queryStatus.get("silver").getAsInt());
            _logger.LogInformation("兑换银瓜子后硬币余额: {0}", silver2coinMoney);

            return silver2coinMoney;
        }
    }
}
