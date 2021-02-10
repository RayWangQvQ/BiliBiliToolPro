using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Console;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Serilog.Sinks.CoolPushBatched;
using Ray.Serilog.Sinks.DingTalkBatched;
using Ray.Serilog.Sinks.TelegramBatched;
using Ray.Serilog.Sinks.WorkWeiXinBatched;
//using Serilog;
using Xunit;

namespace LogTest
{
    public class TestCoolPush
    {
        private string _key;

        public TestCoolPush()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Program.CreateHost(new string[] { });

            _key = Global.ConfigurationRoot["Serilog:WriteTo:7:Args:sKey"];
        }

        [Fact]
        public void Test2()
        {
            //var msg = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"投币\"】---\r\n\r\nℹ 今日已投5枚硬币，已完成投币任务，不需要再投啦~\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"漫画签到\"】---\r\n\r\nℹ 今日已签到过，无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心签到\"】---\r\n\r\nℹ 今日已签到过,无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心银瓜子兑换硬币\"】---\r\n\r\nℹ 银瓜子兑换硬币失败，原因：\"银瓜子余额不足\"\r\n\r\nℹ 当前银瓜子余额: 564\r\n\r\nℹ 当前硬币余额: 672.4\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"每月领取大会员福利\"】---\r\n\r\nℹ 目标领取日期为1号，今天是25号，跳过领取任务\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"每月领取大会员漫画权益\"】---\r\n\r\nℹ 目标领取日期为1号，今天是25号，跳过领取任务\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"每月为自己充电\"】---\r\n\r\nℹ 目标充电日期为31号，今天是25号，跳过充电任务\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ -----全部任务已执行结束-----\r\n\r\n\r\nℹ 开始推送\r\n\r\n";
            //var msg2 = "ℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            //var msg3 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"投币\"】---\r\n\r\nℹ 今日已投5枚硬币，已完成投币任务，不需要再投啦~\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"漫画签到\"】---\r\n\r\nℹ 今日已签到过，无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心签到\"】---\r\n\r\nℹ 今日已签到过,无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心银瓜子兑换硬币\"】---\r\n\r\nℹ 银瓜子兑换硬币失败，原因：\"银瓜子余额不足\"\r\n\r\nℹ 当前银瓜子余额: 564\r\n\r\nℹ 当前硬币余额: 672.4\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"每月领取大会员福利\"】---\r\n\r\nℹ 目标领取日期为1号，今天是25号，跳过领取任务\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"每月领取大会员漫画权益\"】---\r\n\r\nℹ 目标领取日期为1号，今天是25号，跳过领取任务\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ";
            //var msg4 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"投币\"】---\r\n\r\nℹ 今日已投5枚硬币，已完成投币任务，不需要再投啦~\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"漫画签到\"】---\r\n\r\nℹ 今日已签到过，无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心签到\"】---\r\n\r\nℹ 今日已签到过,无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心银瓜子兑换硬币\"】---\r\n\r\nℹ 银瓜子兑换硬币失败，原因：\"银瓜子余额不足\"\r\n\r\nℹ 当前银瓜子余额: 564\r\n\r\nℹ 当前硬币余额: 672.4\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            //var msg5 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"投币\"】---\r\n\r\nℹ 今日已投5枚硬币，已完成投币任务，不需要再投啦~\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"漫画签到\"】---\r\n\r\nℹ 今日已签到过，无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"直播中心签到\"】---\r\n\r\nℹ 今日已签到过,无法重复签到\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            //var msg6 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"投币\"】---\r\n\r\nℹ 今日已投5枚硬币，已完成投币任务，不需要再投啦~\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            //var msg7 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\nℹ ---开始【\"观看、分享视频\"】---\r\n\r\nℹ 今天已经观看过了，不需要再看啦\r\n\r\nℹ 今天已经分享过了，不要再分享啦\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            var msg8 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\nℹ 距离升级到Lv6还有: 261天\r\n\r\nℹ ---结束---\r\n\r\n\r\n";
            //var msg9 = "ℹ 版本号：\"1.0.18\"\r\n\r\nℹ 开源地址：\"https://github.com/RayWangQvQ/BiliBiliTool\"\r\n\r\nℹ 当前环境：\"Development\" \r\n\r\n\r\nℹ -----开始每日任务-----\r\n\r\n\r\nℹ ---开始【\"登录\"】---\r\n\r\nℹ 登录成功，用户名: \"在*楼\"\r\n\r\nℹ 硬币余额: 672.4\r\n\r\n";

            CoolPushApiClient client = new CoolPushApiClient(_key);
            var result = client.PushMessage(msg8);
            Debug.WriteLine(result.Content.ReadAsStringAsync().Result);

            System.Console.ReadLine();
        }
    }
}
