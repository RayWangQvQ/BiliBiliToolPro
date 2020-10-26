using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.DomainService.Interfaces;

namespace Ray.BiliBiliTool.DomainService
{
    public class VideoDomainService : IVideoDomainService
    {
        private readonly ILogger<VideoDomainService> _logger;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly BiliBiliCookiesOptions _biliBiliCookiesOptions;
        private readonly IOptionsMonitor<DailyTaskOptions> _dailyTaskOptions;
        private readonly IAccountApi _accountApi;
        private readonly ICoinDomainService _coinDomainService;

        public VideoDomainService(ILogger<VideoDomainService> logger,
            IDailyTaskApi dailyTaskApi,
            BiliBiliCookiesOptions biliBiliCookiesOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IAccountApi accountApi,
            ICoinDomainService coinDomainService)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookiesOptions = biliBiliCookiesOptions;
            _dailyTaskOptions = dailyTaskOptions;
            _accountApi = accountApi;
            _coinDomainService = coinDomainService;
        }

        public string GetRandomVideo()
        {
            return RegionRanking();
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        public void WatchVideo(string aid)
        {
            int playedTime = new Random().Next(1, 90);
            var apiResponse = _dailyTaskApi.UploadVideoHeartbeat(aid, playedTime).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("av{aid}播放成功,已观看到第{playedTime}秒", aid, playedTime);
                //desp.appendDesp("av" + aid + "播放成功,已观看到第" + playedTime + "秒");
            }
            else
            {
                _logger.LogDebug("av{aid}播放失败,原因：{msg}", aid, apiResponse.Message);
                //desp.appendDesp("av" + aid + "播放成功,已观看到第" + playedTime + "秒");
            }
        }

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid">视频aid</param>
        public void ShareVideo(string aid)
        {
            var apiResponse = _dailyTaskApi.ShareVideo(aid, _biliBiliCookiesOptions.BiliJct).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("视频: av{aid}分享成功", aid);
                //desp.appendDesp("视频: av" + aid + "分享成功");
            }
            else
            {
                _logger.LogDebug("视频分享失败，原因: {msg}", apiResponse.Message);
                _logger.LogDebug("开发者提示: 如果是csrf校验失败请检查BILI_JCT参数是否正确或者失效");
                //desp.appendDesp("重要:csrf校验失败请检查BILI_JCT参数是否正确或者失效");
            }
        }

        /// <summary>
        /// 是否已为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <returns></returns>
        public bool IsDonatedCoinsForVideo(string aid)
        {
            int multiply = _dailyTaskApi.GetDonatedCoinsForVideo(aid).Result.Data.Multiply;
            if (multiply > 0)
            {
                _logger.LogInformation("已经为Av" + aid + "投过" + multiply + "枚硬币啦");
                return true;
            }
            else
            {
                _logger.LogInformation("还没有为Av" + aid + " 投过硬币，开始投币");
                return false;
            }
        }

        /// <summary>
        /// 投币
        /// </summary>
        public void AddCoinsForVideo()
        {
            //投币最多操作数 解决csrf校验失败时死循环的问题
            int addCoinOperateCount = 0;
            //安全检查，最多投币数
            int maxNumberOfCoins = 5;
            //获取自定义配置投币数 配置写在src/main/resources/config.json中
            int setCoin = _dailyTaskOptions.CurrentValue.NumberOfCoins;
            //已投的硬币
            int useCoin = _coinDomainService.GetDonatedCoins();

            //还需要投的币=设置投币数-已投的币数
            if (setCoin > maxNumberOfCoins)
            {
                _logger.LogInformation("自定义投币数为: {setCoin}枚,为保护你的资产，自定义投币数重置为: {maxNumberOfCoins}枚", setCoin, maxNumberOfCoins);
                setCoin = maxNumberOfCoins;
            }

            _logger.LogInformation("自定义投币数为: {setCoin}枚,程序执行前已投: {useCoin}枚", setCoin, useCoin);
            //desp.appendDesp($"自定义投币数为: {setCoin}枚,程序执行前已投: {useCoin}枚");
            int needCoins = setCoin - useCoin;

            //投币前硬币余额
            int coinBalance = _coinDomainService.GetCoinBalance();

            if (needCoins <= 0)
            {
                _logger.LogInformation("已完成设定的投币任务，今日无需再投币了");
            }
            else
            {
                _logger.LogInformation("投币数调整为: {needCoins}枚", needCoins);
                //投币数大于余额时，按余额投
                if (needCoins > coinBalance)
                {
                    _logger.LogInformation("完成今日设定投币任务还需要投: {needCoins}枚硬币，但是余额只有: {coinBalance}", needCoins, coinBalance);
                    _logger.LogInformation("投币数调整为: {coinBalance}", coinBalance);
                    needCoins = coinBalance;
                }
            }

            _logger.LogInformation("投币前余额为 : " + coinBalance);
            //desp.appendDesp("投币前余额为 : " + beforeAddCoinBalance);

            while (needCoins > 0 && needCoins <= maxNumberOfCoins)
            {
                string aid = RegionRanking();
                addCoinOperateCount++;
                _logger.LogInformation("正在为av{aid}投币", aid);
                //desp.appendDesp("正在为av" + aid + "投币");

                bool flag = AddCoinsForVideo(aid, 1, _dailyTaskOptions.CurrentValue.SelectLike);
                if (flag)
                {
                    needCoins--;
                }

                if (addCoinOperateCount > 10)
                {
                    break;
                }
            }

            _logger.LogInformation("投币任务完成后余额为: " + _accountApi.GetCoinBalance().Result.Data.Money);
            //desp.appendDesp("投币任务完成后余额为: " + OftenAPI.getCoinBalance());
        }

        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <param name="multiply">投币数量</param>
        /// <param name="select_like">是否同时点赞 1是0否</param>
        /// <returns>是否投币成功</returns>
        public bool AddCoinsForVideo(string aid, int multiply, int select_like)
        {
            //判断曾经是否对此av投币过
            if (IsDonatedCoinsForVideo(aid))
            {
                _logger.LogDebug("{aid}已经投币过了", aid);
                return false;
            }
            else
            {
                var result = _dailyTaskApi.AddCoinForVideo(aid, multiply, select_like, _biliBiliCookiesOptions.BiliJct).Result;

                if (result != null)
                {
                    _logger.LogInformation("为Av{aid}投币成功", aid);
                    //desp.appendDesp("为Av" + aid + "投币成功");
                    return true;
                }
                else
                {
                    _logger.LogInformation("投币失败");
                    return false;
                }
            }
        }


        #region private
        /// <summary>
        /// 默认请求动画区，3日榜单
        /// </summary>
        /// <returns></returns>
        private string RegionRanking()
        {
            int rid = RandomRegion();
            int day = 3;
            return RegionRanking(rid, day);
        }

        /// <summary>
        /// 从有限分区中随机返回一个分区rid
        /// 后续会更新请求分区
        /// </summary>
        /// <returns>分区Id</returns>
        private int RandomRegion()
        {
            int[] arr = { 1, 3, 4, 5, 160, 22, 119 };
            return arr[new Random().Next(arr.Length - 1)];
        }

        /// <summary>
        /// 获取随机视频aid
        /// </summary>
        /// <param name="rid">分区id</param>
        /// <param name="day">日榜，三日榜 周榜 1，3，7</param>
        /// <returns>随机返回一个aid</returns>
        private string RegionRanking(int rid, int day)
        {
            var apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, day).Result;

            _logger.LogInformation("获取分区:{rid}的{day}日top10榜单成功", rid, day);

            return apiResponse.Data[new Random().Next(apiResponse.Data.Count)].Aid;
        }
        #endregion
    }
}
