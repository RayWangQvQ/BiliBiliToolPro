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

        public CoinDomainService(ILogger<CoinDomainService> logger,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<BiliBiliCookieOptions> biliBiliCookieOptions,
            IAccountApi accountApi)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _biliBiliCookieOptions = biliBiliCookieOptions.CurrentValue;
            _accountApi = accountApi;
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
            //var result = _experienceApi.GetDonateCoinExp().Result;
            //todo:这里使用Refit调用，连接、获取成功(Status=200)，但是从Content获取Data异常，怀疑和返回内容被gzip压缩有关，但是暂未找到解决办法，下面先通过手动调用手动解压实现

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", _biliBiliCookieOptions.ToString());

            HttpResponseMessage result = client.GetAsync(ApiList.needCoin).Result;
            var data = result.Content.ReadAsByteArrayAsync().Result;
            var dataStr = ZipHelper.ReadGzip(data);

            _logger.LogDebug("调用获取今日投币经验返回: {0}", dataStr);

            ExperienceByDonateCoin re = JsonSerializer.Deserialize<ExperienceByDonateCoin>(dataStr);

            _logger.LogDebug("今日已获得投币经验: " + re.Number);
            return re.Number;
        }
        #endregion
    }
}
