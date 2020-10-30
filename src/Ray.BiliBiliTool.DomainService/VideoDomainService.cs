using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Dtos;
using Ray.BiliBiliTool.Agent.Interfaces;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.DomainService.Attributes;
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
            IOptionsMonitor<BiliBiliCookiesOptions> biliBiliCookiesOptions,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IAccountApi accountApi,
            ICoinDomainService coinDomainService)
        {
            _logger = logger;
            _dailyTaskApi = dailyTaskApi;
            _biliBiliCookiesOptions = biliBiliCookiesOptions.CurrentValue;
            _dailyTaskOptions = dailyTaskOptions;
            _accountApi = accountApi;
            _coinDomainService = coinDomainService;
        }

        /// <summary>
        /// 获取随机视频
        /// </summary>
        /// <returns></returns>
        //[LogIntercepter("获取随机视频")]
        public string GetRandomVideo()
        {
            string aid = RegionRanking();
            return aid;
        }

        /// <summary>
        /// 观看视频
        /// </summary>
        [LogIntercepter("观看视频")]
        public void WatchVideo(string aid, DailyTaskInfo dailyTaskStatus)
        {
            if (dailyTaskStatus.Watch)
            {
                _logger.LogInformation("本日观看视频任务已经完成了，不需要再观看视频了");
                return;
            }

            int playedTime = new Random().Next(1, 90);
            var apiResponse = _dailyTaskApi.UploadVideoHeartbeat(aid, playedTime).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("av{aid}播放成功,已观看到第{playedTime}秒", aid, playedTime);
            }
            else
            {
                _logger.LogDebug("av{aid}播放失败,原因：{msg}", aid, apiResponse.Message);
            }
        }

        /// <summary>
        /// 分享视频
        /// </summary>
        /// <param name="aid">视频aid</param>
        [LogIntercepter("分享视频")]
        public void ShareVideo(string aid, DailyTaskInfo dailyTaskStatus)
        {
            if (dailyTaskStatus.Share)
            {
                _logger.LogInformation("本日分享视频任务已经完成了，不需要再分享视频了");
                return;
            }

            var apiResponse = _dailyTaskApi.ShareVideo(aid, _biliBiliCookiesOptions.BiliJct).Result;

            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("视频: av{aid}分享成功", aid);
            }
            else
            {
                _logger.LogDebug("视频分享失败，原因: {msg}", apiResponse.Message);
                _logger.LogDebug("开发者提示: 如果是csrf校验失败请检查BILI_JCT参数是否正确或者失效");
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
                //_logger.LogInformation("已经为Av" + aid + "投过" + multiply + "枚硬币啦");
                return true;
            }
            else
            {
                //_logger.LogInformation("还没有为Av" + aid + " 投过硬币，开始投币");
                return false;
            }
        }

        /// <summary>
        /// 投币
        /// </summary>
        [LogIntercepter("投币")]
        public void AddCoinsForVideo()
        {
            int needCoins = GetNeedDonateCoins(out int alreadyCoins, out int targetCoins);
            _logger.LogInformation("今日已投{already}枚硬币，目标是投{target}枚硬币", alreadyCoins, targetCoins);

            if (needCoins <= 0)
            {
                _logger.LogInformation("已完成投币任务，今天不需要再投啦");
                return;
            }

            _logger.LogInformation("还需再投{need}枚硬币", needCoins);

            //投币前硬币余额
            int coinBalance = _coinDomainService.GetCoinBalance();
            _logger.LogInformation("投币前余额为 : " + coinBalance);

            if (coinBalance <= 0)
            {
                _logger.LogInformation("因硬币余额不足，今日暂不执行投币任务");
                return;
            }

            //余额小于目标投币数，按余额投
            if (coinBalance < needCoins)
            {
                _logger.LogInformation("因硬币余额不足，目标投币数调整为: {coinBalance}", coinBalance);
                needCoins = coinBalance;
            }

            int successCoins = 0;
            int tryCount = 0;//投币最多操作数 解决csrf校验失败时死循环的问题
            while (successCoins < needCoins)
            {
                tryCount++;

                string aid = RegionRanking();//todo：只调用一次获取视频集合接口，一次性获取足够数量的视频，避免每次都获取视频
                _logger.LogDebug("正在为av{aid}投币", aid);

                bool isSuccess = AddCoinsForVideo(aid, 1, _dailyTaskOptions.CurrentValue.SelectLike);
                if (isSuccess)
                {
                    successCoins++;
                }

                if (tryCount > 10)
                {
                    _logger.LogInformation("尝试投币次数超过10次，投币任务终止");
                    break;
                }
            }

            _logger.LogInformation("投币任务完成后余额为: " + _accountApi.GetCoinBalance().Result.Data.Money);
        }

        /// <summary>
        /// 为视频投币
        /// </summary>
        /// <param name="aid">av号</param>
        /// <param name="multiply">投币数量</param>
        /// <param name="select_like">是否同时点赞 1是0否</param>
        /// <returns>是否投币成功</returns>
        public bool AddCoinsForVideo(string aid, int multiply, bool select_like)
        {
            //判断曾经是否对此av投币过
            if (IsDonatedCoinsForVideo(aid))
            {
                _logger.LogDebug("{aid}已经投币过了", aid);
                return false;
            }

            var result = _dailyTaskApi.AddCoinForVideo(aid, multiply, select_like ? 1 : 0, _biliBiliCookiesOptions.BiliJct).Result;

            if (result != null)//todo：
            {
                _logger.LogInformation("为Av{aid}投币成功", aid);//todo:视频名称
                return true;
            }
            else
            {
                _logger.LogInformation("为Av{aid}投币失败", aid);
                return false;
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

            //_logger.LogInformation("获取分区:{rid}的{day}日top10榜单成功", rid, day);

            return apiResponse.Data[new Random().Next(apiResponse.Data.Count)].Aid;
        }

        /// <summary>
        /// 获取今日的目标投币数
        /// </summary>
        /// <param name="alreadyCoins"></param>
        /// <param name="targetCoins"></param>
        /// <returns></returns>
        private int GetNeedDonateCoins(out int alreadyCoins, out int targetCoins)
        {
            int needCoins = 0;

            //获取自定义配置投币数
            int configCoins = _dailyTaskOptions.CurrentValue.NumberOfCoins;
            //已投的硬币
            alreadyCoins = _coinDomainService.GetDonatedCoins();
            //目标
            targetCoins = configCoins > Constants.MaxNumberOfDonateCoins
                ? Constants.MaxNumberOfDonateCoins
                : configCoins;

            if (targetCoins > alreadyCoins)
            {
                return targetCoins - alreadyCoins;
            }
            return needCoins;
        }
        #endregion
    }
}
