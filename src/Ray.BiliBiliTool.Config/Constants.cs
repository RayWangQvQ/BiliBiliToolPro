using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Config
{
    public static class Constants
    {
        /// <summary>
        /// 命令行启动时的参数映射
        /// </summary>
        public static readonly Dictionary<string, string> CommandLineMapper = new Dictionary<string, string>
        {
            {"-userId","BiliBiliCookie:UserId" },
            {"-sessData","BiliBiliCookie:SessData" },
            {"-biliJct","BiliBiliCookie:BiliJct" },

            {"-numberOfCoins","DailyTaskConfig:NumberOfCoins" },
            {"-selectLike","DailyTaskConfig:SelectLike" },
            {"-supportUpIds","DailyTaskConfig:SupportUpIds" },
            {"-dayOfAutoCharge","DailyTaskConfig:DayOfAutoCharge" },
            {"-dayOfReceiveVipPrivilege","DailyTaskConfig:DayOfReceiveVipPrivilege" },
            {"-devicePlatform","DailyTaskConfig:DevicePlatform" },

            {"-intervalSecondsBetweenRequestApi","Security:IntervalSecondsBetweenRequestApi" },
            {"-intervalMethodTypes", "Security:IntervalMethodTypes"},

            {"-pushScKey","Push:PushScKey" },

            {"-closeConsoleWhenEnd","CloseConsoleWhenEnd" }
        };

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
        public static string SourceCodeUrl = "https://github.com/RayWangQvQ/BiliBiliTool";
    }
}
