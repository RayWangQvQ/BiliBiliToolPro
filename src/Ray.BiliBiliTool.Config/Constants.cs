using System.Collections.Generic;

namespace Ray.BiliBiliTool.Config;

public static class Constants
{
    /// <summary>
    /// 每天的最大投币数，优先级最高，默认每天最多投5个币（包含已投过的数量）
    /// </summary>
    public static int MaxNumberOfDonateCoins = 5;

    /// <summary>
    /// 每天可获取的满额经验值
    /// </summary>
    public static int EveryDayExp = 65;

    /// <summary>
    /// 开源地址
    /// </summary>
    public static string SourceCodeUrl = "https://github.com/RayWangQvQ/BiliBiliToolPro";

    /// <summary>
    /// 每日任务exp
    /// </summary>
    /// <returns></returns>
    public static readonly Dictionary<string, int> ExpDic = new()
    {
        { "每日登录", 5 },
        { "每日观看视频", 5 },
        { "每日分享视频", 5 },
        { "每日投币", 10 },
    };

    /// <summary>
    /// 投币接口的data.code返回以下这些状态码，则可以继续尝试投币<para></para>
    /// 如返回除这些之外的状态码，则终止投币流程，不进行无意义的尝试<para></para>
    /// （比如返回-101：账号未登录；-102：账号被封停；-111：csrf校验失败等）
    /// </summary>
    /// <returns></returns>
    public static readonly Dictionary<string, string> DonateCoinCanContinueStatusDic = new()
    {
        { "0", "成功" },
        { "-400", "请求错误" },
        { "10003", "不存在该稿件" },
        { "34002", "不能给自己投币" },
        { "34003", "非法的投币数量" },
        { "34004", "投币间隔太短" },
        { "34005", "超过投币上限" },
    };

    public static readonly Dictionary<string, string> CommandLineMappingsDic = new()
    {
        { "--cookieStr1", "BiliBiliCookies:1" },
        { "--runTasks", "RunTasks" },
        { "--randomSleep", "Security:RandomSleepMaxMin" },
        { "--numberOfCoins", "DailyTaskConfig:NumberOfCoins" },
        { "--numberOfProtectedCoins", "DailyTaskConfig:NumberOfProtectedCoins" },
        { "--saveCoinsWhenLv6", "DailyTaskConfig:SaveCoinsWhenLv6" },
        { "--selectLike", "DailyTaskConfig:SelectLike" },
        { "--supportUpIds", "DailyTaskConfig:SupportUpIds" },
        { "--dayOfAutoCharge", "DailyTaskConfig:DayOfAutoCharge" },
        { "--autoChargeUpId", "DailyTaskConfig:AutoChargeUpId" },
        { "--dayOfReceiveVipPrivilege", "DailyTaskConfig:DayOfReceiveVipPrivilege" },
        { "--isExchangeSilver2Coin", "DailyTaskConfig:IsExchangeSilver2Coin" },
        { "--devicePlatform", "DailyTaskConfig:DevicePlatform" },
        { "--excludeAwardNames", "LiveLotteryTaskConfig:ExcludeAwardNames" },
        { "--includeAwardNames", "LiveLotteryTaskConfig:INCLUDEAWARDNAMES" },
        { "--unfollowGroup", "UnfollowBatchedTaskConfig:GroupName" },
        { "--unfollowCount", "UnfollowBatchedTaskConfig:Count" },
        { "--intervalSecondsBetweenRequestApi", "Security:IntervalSecondsBetweenRequestApi" },
        { "--intervalMethodTypes", "Security:IntervalMethodTypes" },
        { "--pushScKey", "Serilog:WriteTo:6:Args:scKey" },
        { "--proxy", "WebProxy" },
    };
}
