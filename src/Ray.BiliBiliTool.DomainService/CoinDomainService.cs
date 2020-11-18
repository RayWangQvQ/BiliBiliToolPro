using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.DomainService
{
    /// <summary>
    /// 硬币
    /// </summary>
    public class CoinDomainService : ICoinDomainService
    {
        private readonly ILogger<CoinDomainService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BiliBiliCookieOptions _biliBiliCookieOptions;
        private readonly IAccountApi _accountApi;
        private readonly IDailyTaskApi _dailyTaskApi;

        public CoinDomainService(ILogger<CoinDomainService> logger,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IAccountApi accountApi,
            IDailyTaskApi dailyTaskApi)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
            _accountApi = accountApi;
            _dailyTaskApi = dailyTaskApi;
        }

        /// <summary>
        /// 获取账户硬币余额
        /// </summary>
        /// <returns></returns>
        public decimal GetCoinBalance()
        {
            var response = _accountApi.GetCoinBalance().Result;
            return response.Data.Money ?? 0;
        }

        /// <summary>
        /// 获取今日已投币数
        /// </summary>
        /// <returns></returns>
        public int GetDonatedCoins()
        {
            return GetDonateCoinExp() / 10;
        }

        #region private
        /// <summary>
        /// 获取今日通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        private int GetDonateCoinExp()
        {
            return _dailyTaskApi.GetDonateCoinExp().Result.Data;
        }
        #endregion
    }
}
