using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Config.Options
{
    public class BiliBiliCookiesOptions
    {
        public string UserId { get; set; }

        public string SessData { get; set; }

        public string BiliJct { get; set; }

        public bool Check(ILogger logger)
        {
            bool result = true;
            string msg = "配置[{0}]为空,该项为必须配置,对应浏览器中Cookie中的[{1}]值";

            if (string.IsNullOrWhiteSpace(UserId))
            {
                logger.LogWarning(msg, "UserId", "DEDEUSERID");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(SessData))
            {
                logger.LogWarning(msg, "SessData", "SESSDATA");
                result = false;
            }
            if (string.IsNullOrWhiteSpace(BiliJct))
            {
                logger.LogWarning(msg, "BiliJct", "BILI_JCT");
                result = false;
            }

            return result;
        }


        public override string ToString()
        {
            return $"bili_jct={BiliJct};SESSDATA={SessData};DedeUserID={UserId}";
        }
    }
}
