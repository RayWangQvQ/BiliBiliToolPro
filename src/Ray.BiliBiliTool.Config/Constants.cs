
namespace Ray.BiliBiliTool.Config
{
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

        public static class OptionsNames
        {
            public static string ExpDictionaryName = "ExpDictionary";

            public static string DonateCoinCanContinueStatusDictionaryName = "DonateCoinCanContinueStatusDictionary";
        }
    }
}
