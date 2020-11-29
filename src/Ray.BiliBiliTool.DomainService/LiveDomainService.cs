using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
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
            var response = _liveApi.Sign().Result;

            if (response.Code == 0)
            {
                _logger.LogInformation($"直播签到成功，本次签到获得{response.Data.Text},{response.Data.SpecialText}");
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
        public decimal ExchangeSilver2Coin()
        {
            var response = _liveApi.ExchangeSilver2Coin().Result;
            if (response.Code == 0)
            {
                _logger.LogInformation("银瓜子兑换硬币成功");
            }
            else
            {
                _logger.LogInformation("银瓜子兑换硬币失败，原因：{0}", response.Message);
            }

            var queryStatus = _liveApi.GetExchangeSilverStatus().Result;
            _logger.LogInformation("当前银瓜子余额: {0}", queryStatus.Data.Silver);

            var silver2CoinMoney = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("当前硬币余额: {0}", silver2CoinMoney);

            return silver2CoinMoney;
        }
    }
}
