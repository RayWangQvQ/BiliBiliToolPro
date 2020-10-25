using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BiliBiliTool.Agent;
using BiliBiliTool.Agent.Interfaces;
using BiliBiliTool.Apiquery;
using BiliBiliTool.Config;
using BiliBiliTool.Login;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Console.Agent;
using Ray.BiliBiliTool.Console.Agent.Interfaces;
using Ray.BiliBiliTool.Console.Helpers;

namespace BiliBiliTool.Task
{
    public class DailyTask
    {
        private readonly ILogger<DailyTask> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Verify _verify;
        private readonly IOptionsMonitor<DailyTaskOptions> _dailyTaskOptions;
        private readonly IDailyTaskApi _dailyTaskApi;
        private readonly IMangaApi _mangaApi;
        private readonly IExperienceApi _experienceApi;
        private readonly IAccountApi _accountApi;
        private readonly ILiveApi _liveApi;

        //AppendPushMsg desp = AppendPushMsg.getInstance();
        //Data userInfo = null;

        public DailyTask(
            ILogger<DailyTask> logger,
            IHttpClientFactory httpClientFactory,
            Verify verify,
            LoginResponse loginResponse,
            IOptionsMonitor<DailyTaskOptions> dailyTaskOptions,
            IDailyTaskApi dailyTaskApi,
            IMangaApi mangaApi,
            IExperienceApi experienceApi,
            IAccountApi accountApi,
            ILiveApi liveApi)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _verify = verify;
            LoginResponse = loginResponse;
            _dailyTaskOptions = dailyTaskOptions;
            _dailyTaskApi = dailyTaskApi;
            _mangaApi = mangaApi;
            _experienceApi = experienceApi;
            _accountApi = accountApi;
            _liveApi = liveApi;
        }

        private LoginResponse LoginResponse { get; set; }

        public void DoDailyTask()
        {
            //登录
            Login();

            DailyTaskInfo dailyTaskStatus = GetDailyTaskStatus();
            string videoAid = GetRandomVideo();

            //观看视频
            if (!dailyTaskStatus.Watch)
                WatchVideo(videoAid); //观看视频 默认会调用分享
            else
                _logger.LogInformation("本日观看视频任务已经完成了，不需要再观看视频了");

            //分享视频
            if (!dailyTaskStatus.Share)
                ShareVideo(videoAid);
            else
                _logger.LogInformation("本日分享视频任务已经完成了，不需要再分享视频了");

            //漫画签到
            MangaSign();

            //投币任务
            AddCoinsForVideo();//todo:传入up主Id，只为指定ups投币

            //直播中心的银瓜子兑换硬币
            ExchangeSilver2Coin();

            //直播中心签到
            LiveSign();

            //月初领取大会员福利
            ReceiveVipPrivilege();

            //月底充电
            doCharge();
            /*
            
            mangaGetVipReward(1);

            _logger.LogInformation("本日任务已全部执行完毕");

            doServerPush();
            */
        }

        /// <summary>
        /// 登录
        /// </summary>
        public void Login()
        {
            var apiResponse = _dailyTaskApi.LoginByCookie().Result;

            if (apiResponse.Code != 0 || !apiResponse.Data.IsLogin)
            {
                _logger.LogWarning("登录异常，Cookies可能失效了,请仔细检查Github Secrets中DEDEUSERID SESSDATA BILI_JCT三项的值是否正确");
                return;
            }

            _logger.LogInformation("登录成功");

            LoginResponse = apiResponse.Data;

            //用户名模糊处理 @happy88888
            _logger.LogInformation("用户名称: {0}", LoginResponse.GetFuzzyUname());
            _logger.LogInformation("硬币余额: " + LoginResponse.Money);

            if (LoginResponse.Level_info.Current_level < 6)
            {
                _logger.LogInformation("距离升级到Lv{0}还有: {1}天",
                    LoginResponse.Level_info.Current_level + 1,
                    (LoginResponse.Level_info.Next_exp - LoginResponse.Level_info.Current_exp) / 65);
            }
            else
            {
                _logger.LogInformation("当前等级Lv6，经验值为：" + LoginResponse.Level_info.Current_exp);
            }
        }

        /// <summary>
        /// 获取每日任务完成情况
        /// </summary>
        /// <returns></returns>
        public DailyTaskInfo GetDailyTaskStatus()
        {
            var apiResponse = _dailyTaskApi.GetDailyTaskRewardInfo().Result;
            if (apiResponse.Code == 0)
            {
                _logger.LogInformation("请求本日任务完成状态成功");
                //desp.appendDesp("请求本日任务完成状态成功");
                return apiResponse.Data;
            }
            else
            {
                _logger.LogDebug(JsonSerializer.Serialize(apiResponse));
                return _dailyTaskApi.GetDailyTaskRewardInfo().Result.Data;
                //todo:偶发性请求失败，再请求一次
            }
        }

        #region 获取随机视频
        public string GetRandomVideo()
        {
            return regionRanking();
        }

        /// <summary>
        /// 默认请求动画区，3日榜单
        /// </summary>
        /// <returns></returns>
        private string regionRanking()
        {
            int rid = randomRegion();
            int day = 3;
            return regionRanking(rid, day);
        }

        /// <summary>
        /// 从有限分区中随机返回一个分区rid
        /// 后续会更新请求分区
        /// </summary>
        /// <returns>分区Id</returns>
        private int randomRegion()
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
        private string regionRanking(int rid, int day)
        {
            var apiResponse = _dailyTaskApi.GetRegionRankingVideos(rid, day).Result;

            _logger.LogInformation("获取分区:{rid}的{day}日top10榜单成功", rid, day);

            return apiResponse.Data[new Random().Next(apiResponse.Data.Count)].Aid;
        }
        #endregion

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
            var apiResponse = _dailyTaskApi.ShareVideo(aid, _verify.BiliJct).Result;

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

        #region 漫画
        /// <summary>
        /// 漫画签到
        /// </summary>
        public void MangaSign()
        {
            var response = _mangaApi.ClockIn(_dailyTaskOptions.CurrentValue.DevicePlatform).Result;

            if (response == null)
            {
                _logger.LogInformation("哔哩哔哩漫画已经签到过了");
                //desp.appendDesp("哔哩哔哩漫画已经签到过了");
                return;
            }

            if (response.Code == 0)
            {
                _logger.LogInformation("完成漫画签到");
                //desp.appendDesp("完成漫画签到");
            }
            else
            {
                _logger.LogInformation("漫画签到异常");
                //desp.appendDesp("完成漫画签到");
            }
        }
        #endregion

        #region 视频投币
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
            int useCoin = GetDonatedCoins();

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
            int coinBalance = GetCoinBalance();

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

            /*
             * 开始投币
             * 请勿修改 max_numberOfCoins 这里多判断一次保证投币数超过5时 不执行投币操作
             * 最后一道安全判断，保证即使前面的判断逻辑错了，也不至于发生投币事故
             */
            while (needCoins > 0 && needCoins <= maxNumberOfCoins)
            {
                string aid = regionRanking();
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
        /// 获取今日已投币数
        /// </summary>
        /// <returns></returns>
        public int GetDonatedCoins()
        {
            return GetDonateCoinExp() / 10;
        }

        /// <summary>
        /// 获取今日通过投币已获取的经验值
        /// </summary>
        /// <returns></returns>
        private int GetDonateCoinExp()
        {
            //var result = _experienceApi.GetDonateCoinExp().Result;
            //todo:这里使用Refit调用，连接、获取成功(Status=200)，但是从Content获取Data异常，确定问题为返回内容被gzip压缩，但是暂未找到解决办法，下面先通过手动调用手动解压实现

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", _verify.getVerify());

            HttpResponseMessage result = client.GetAsync(ApiList.needCoin).Result;
            var data = result.Content.ReadAsByteArrayAsync().Result;
            var dataStr = ZipHelper.ReadGzip(data);

            ExperienceByDonateCoin re = JsonSerializer.Deserialize<ExperienceByDonateCoin>(dataStr);

            _logger.LogInformation("今日已获得投币经验: " + re.Number);
            return re.Number;
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
                var result = _dailyTaskApi.AddCoinForVideo(aid, multiply, select_like, _verify.BiliJct).Result;

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
        #endregion

        /// <summary>
        /// 获取账户硬币余额
        /// </summary>
        /// <returns></returns>
        public int GetCoinBalance()
        {
            var response = _accountApi.GetCoinBalance().Result;
            return response.Data.Money;
        }

        #region 直播中心签到、银瓜子兑换B币
        /// <summary>
        /// 直播中心银瓜子兑换B币
        /// </summary>
        public void ExchangeSilver2Coin()
        {
            var response = _liveApi.ExchangeSilver2Coin().Result;
            if (response.Code == 0)
            {
                _logger.LogInformation("银瓜子兑换硬币成功");
                //desp.appendDesp("银瓜子兑换硬币成功");
            }
            else
            {
                _logger.LogDebug("银瓜子兑换硬币失败，原因：{0}", response.Message);
                //desp.appendDesp("银瓜子兑换硬币失败 原因是: " + resultJson.get("msg").getAsstring());
            }

            var queryStatus = _liveApi.GetExchangeSilverStatus().Result;
            double silver2coinMoney = GetCoinBalance();

            _logger.LogInformation("当前银瓜子余额: {0}", queryStatus.Data.Silver);
            //desp.appendDesp("当前银瓜子余额: " + queryStatus.get("silver").getAsInt());
            _logger.LogInformation("兑换银瓜子后硬币余额: {0}", silver2coinMoney);

            /*
            兑换银瓜子后，更新userInfo中的硬币值
             */
            //userInfo.setMoney(silver2coinMoney);

        }

        /// <summary>
        /// 直播签到
        /// </summary>
        public void LiveSign()
        {
            _logger.LogInformation("开始直播签到");

            //JsonObject liveCheckinResponse = HttpUnit.doGet(ApiList.liveCheckin);
            //int code = liveCheckinResponse.get(statusCodeStr).getAsInt();

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
        #endregion

        #region 充电
        /// <summary>
        /// 月底自动给自己充电
        /// 仅充会到期的B币券，低于2的时候不会充
        /// </summary>
        public void doCharge()
        {
            if (!_dailyTaskOptions.CurrentValue.MonthEndAutoCharge) return;

            int lastDay = GetLastDayOfMonth(DateTime.Today).Day;
            if (DateTime.Today.Day != lastDay)
            {
                _logger.LogInformation($"今天是本月的第: {DateTime.Today.Day}天，等到{lastDay}号会自动为您充电哒");
                //desp.appendDesp("今天是本月的第: " + day + "天，还没到给自己充电日子呢");
                return;
            }

            //B币券余额
            int couponBalance = LoginResponse.Wallet.Coupon_balance;
            if (couponBalance < 2)
            {
                _logger.LogInformation("B币券余额<2,无法充电");
                return;
            }

            //大会员类型
            int vipType = queryVipStatusType();
            if (vipType != 2)
            {
                _logger.LogInformation("不是年度大会员或已过期,无法充电");
                return;
            }

            /*
              判断条件 是月底&&是年大会员&&b币券余额大于2&&配置项允许自动充电
             */

            //string requestBody = $"elec_num={couponBalance * 10}&up_mid={userId}&otype=up&oid={userId}&csrf={_verify.BiliJct}";
            //JsonObject jsonObject = HttpUnit.doPost(ApiList.autoCharge, requestBody);
            //int resultCode = jsonObject.get("code").getAsInt();

            var response = _dailyTaskApi.Charge(couponBalance * 10, _verify.UserId, _verify.UserId, _verify.BiliJct).Result;
            if (response.Code == 0)
            {
                if (response.Data.Status == 4)
                {
                    _logger.LogInformation("月底了，给自己充电成功啦，送的B币券没有浪费哦");
                    _logger.LogInformation("本次给自己充值了: " + couponBalance * 10 + "个电池哦");
                    //desp.appendDesp("本次给自己充值了: " + couponBalance * 10 + "个电池哦");

                    //获取充电留言token
                    chargeComments(response.Data.Order_no);
                }
                else
                {
                    _logger.LogDebug("充电失败了啊 原因: " + JsonSerializer.Serialize(response));
                }
            }
            else
            {
                _logger.LogDebug("充电失败了啊 原因: " + response.Message);
            }
        }

        public DateTime GetLastDayOfMonth(DateTime dateTime)
        {
            return dateTime.AddDays(1 - dateTime.Day)
                .AddMonths(1)
                .AddDays(-1);
        }

        /// <summary>
        /// 每月1号领取大会员福利
        /// </summary>
        public void ReceiveVipPrivilege()
        {
            int day = DateTime.Today.Day;

            //大会员类型
            int vipType = queryVipStatusType();

            if (day == 1 && vipType == 2)
            {
                ReceiveVipPrivilege(1);
                ReceiveVipPrivilege(2);
            }

            if (vipType == 0 || vipType == 1)
            {
                _logger.LogInformation("普通会员和月度大会员每月不赠送B币券，所以没法给自己充电哦");
                return;
            }
        }

        /// <summary>
        /// 领取大会员每月赠送福利
        /// </summary>
        /// <param name="type">1.大会员B币券；2.大会员福利</param>
        public void ReceiveVipPrivilege(int type)
        {
            var response = _dailyTaskApi.ReceiveVipPrivilege(type, _verify.BiliJct).Result;
            if (response.Code == 0)
            {
                if (type == 1)
                {
                    _logger.LogInformation("领取年度大会员每月赠送的B币券成功");
                }
                else if (type == 2)
                {
                    _logger.LogInformation("领取大会员福利/权益成功");
                }
            }
            else
            {
                _logger.LogDebug($"领取年度大会员每月赠送的B币券/大会员福利失败，原因: {response.Message}");
            }
        }

        /**
         * @return 返回会员类型
         * 0:无会员（会员过期、当前不是会员）
         * 1:月会员
         * 2:年会员
         */
        public int queryVipStatusType()
        {
            if (LoginResponse.VipStatus == 1)
            {
                //只有VipStatus为1的时候获取到VipType才是有效的。
                return LoginResponse.VipType;
            }
            else
            {
                return 0;
            }
        }

        public void chargeComments(string token)
        {
            //string requestBody = $"order_id={token}&message=BILIBILI-HELPER自动充电&csrf={_verify.BiliJct}";
            //JsonObject jsonObject = HttpUnit.doPost(ApiList.chargeComment, requestBody);

            _dailyTaskApi.ChargeComment(token, "Ray.BiliBiliTool自动充电", _verify.BiliJct);
        }
        #endregion


        #region 
        ///**
        // * 获取大会员漫画权益
        // *
        // * @param reason_id 权益号，由https://api.bilibili.com/x/vip/privilege/my
        // *                  得到权益号数组，取值范围为数组中的整数
        // *                  为方便直接取1，为领取漫读劵，暂时不取其他的值
        // * @return 返回领取结果和数量
        // */
        //public void mangaGetVipReward(int reason_id)
        //{

        //    Calendar cal = Calendar.getInstance(TimeZone.getTimeZone("GMT+8"));
        //    int day = cal.get(Calendar.DATE);

        //    //根据userInfo.getVipStatus() ,如果是1 ，会员有效，0会员失效。
        //    //@JunzhouLiu: fixed query_vipStatusType()现在可以查询会员状态，以及会员类型了 2020-10-15
        //    if (day != 1 || queryVipStatusType() == 0)
        //    {
        //        //一个月执行一次就行，跟几号没关系，由B站策略决定(有可能改领取时间)
        //        return;
        //    }

        //    string requestBody = "{\"reason_id\":" + reason_id + "}";
        //    //注意参数构造格式为json，不知道需不需要重载下面的Post函数改请求头
        //    JsonObject jsonObject = HttpUnit.doPost(ApiList.mangaGetVipReward, requestBody);
        //    if (jsonObject.get(statusCodeStr).getAsInt() == 0)
        //    {
        //        //@happy888888:好像也可以getAsstring或,getAsShort
        //        //@JunzhouLiu:Int比较好判断
        //        _logger.LogInformation("大会员成功领取" + jsonObject.get("data").getAsJsonObject().get("amount").getAsInt() + "张漫读劵");
        //    }
        //    else
        //    {
        //        _logger.LogInformation("大会员领取漫读劵失败，原因为:" + jsonObject.get("msg").getAsstring());
        //    }
        //}




        //public void doServerPush()
        //{
        //    if (ServerVerify.getMsgPushKey() != null)
        //    {
        //        ServerPush serverPush = new ServerPush();
        //        serverPush.pushMsg("BILIBILIHELPER任务简报", desp.getPushDesp());
        //    }
        //    else
        //    {
        //        _logger.LogInformation("未配置server酱,本次执行不推送日志到微信");
        //    }

        //}

        #endregion
    }
}
