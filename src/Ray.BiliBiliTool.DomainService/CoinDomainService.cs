using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Helpers;

namespace Ray.BiliBiliTool.DomainService
{
    public class CoinDomainService : ICoinDomainService
    {
        private readonly ILogger<CoinDomainService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BiliBiliCookiesOptions _biliBiliCookiesOptions;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly IAccountApi _accountApi;

        public CoinDomainService(ILogger<CoinDomainService> logger,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<BiliBiliCookiesOptions> biliBiliCookiesOptions,
            IDailyTaskApi dailyTaskApi,
            IAccountApi accountApi)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _biliBiliCookiesOptions = biliBiliCookiesOptions.CurrentValue;
            _dailyTaskApi = dailyTaskApi;
            _accountApi = accountApi;
        }

        /// <summary>
        /// 获取账户硬币余额
        /// </summary>
        /// <returns></returns>
        public int GetCoinBalance()
        {
            var response = _accountApi.GetCoinBalance().Result;
            return response.Data.Money;
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
            //var result = _experienceApi.GetDonateCoinExp().Result;
            //todo:这里使用Refit调用，连接、获取成功(Status=200)，但是从Content获取Data异常，确定问题为返回内容被gzip压缩，但是暂未找到解决办法，下面先通过手动调用手动解压实现

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", _biliBiliCookiesOptions.ToString());

            HttpResponseMessage result = client.GetAsync(ApiList.needCoin).Result;
            var data = result.Content.ReadAsByteArrayAsync().Result;
            var dataStr = ZipHelper.ReadGzip(data);

            ExperienceByDonateCoin re = JsonSerializer.Deserialize<ExperienceByDonateCoin>(dataStr);

            _logger.LogInformation("今日已获得投币经验: " + re.Number);
            return re.Number;
        }
        #endregion
    }
}
