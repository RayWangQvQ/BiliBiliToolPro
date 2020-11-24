using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Extensions;

namespace Ray.BiliBiliTool.Config.Options
{
    /// <summary>
    /// Cookie信息
    /// </summary>
    public class BiliBiliCookieOptions
    {
        private string _userId;
        private string _biliJct;

        /// <summary>
        /// DedeUserID
        /// </summary>
        [Description("DedeUserID")]
        public string UserId
        {
            get =>
                !string.IsNullOrWhiteSpace(_userId)
                    ? _userId
                    : RayConfiguration.Root["BiliBiliCookie:DedeUserID"];//为了兼容 GitHub Secrets 经常会被填错
            set => _userId = value;
        }

        /// <summary>
        /// SESSDATA
        /// </summary>
        [Description("SESSDATA")]
        public string SessData { get; set; }

        /// <summary>
        /// bili_jct
        /// </summary>
        [Description("bili_jct")]
        public string BiliJct
        {
            get =>
                !string.IsNullOrWhiteSpace(_biliJct)
                    ? _biliJct
                    : RayConfiguration.Root["BiliBiliCookie:Bili_jct"];//为了兼容 GitHub Secrets 经常会被填错
            set => _biliJct = value;
        }

        /// <summary>
        /// 检查是否已配置
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public bool Check(ILogger logger)
        {
            bool result = true;
            string msg = "配置项[{0}]为空，该项为必须配置，对应浏览器中Cookie中的[{1}]值";
            string tips = "检测到已配置了[{0}]，已兼容使用[{1}]";

            //UserId为空，抛异常
            if (string.IsNullOrWhiteSpace(UserId))
            {
                logger.LogWarning(msg, nameof(UserId), GetPropertyDescription(nameof(UserId)));

                result = false;
            }
            else if (!long.TryParse(UserId, out long uid))//不为空，但不能转换为long，警告
            {
                logger.LogWarning("UserId：{uid} 不能转换为long型，请确认配置的是正确的Cookie值");
            }
            //UserId为空，但DedeUserID有值，兼容使用
            if (string.IsNullOrWhiteSpace(RayConfiguration.Root["BiliBiliCookie:UserID"])
                && !string.IsNullOrWhiteSpace(RayConfiguration.Root["BiliBiliCookie:DedeUserID"]))
            {
                logger.LogWarning(tips, "DEDEUSERID", "DEDEUSERID");
            }

            //SessData为空，抛异常
            if (string.IsNullOrWhiteSpace(SessData))
            {
                logger.LogWarning(msg, nameof(SessData), GetPropertyDescription(nameof(SessData)));
                result = false;
            }

            //BiliJct为空，抛异常
            if (string.IsNullOrWhiteSpace(BiliJct))
            {
                logger.LogWarning(msg, nameof(BiliJct), GetPropertyDescription(nameof(BiliJct)));
                result = false;
            }
            //BiliJct为空，但Bili_jct有值，兼容使用
            else if (string.IsNullOrWhiteSpace(RayConfiguration.Root["BiliBiliCookie:BiliJct"])
                && !string.IsNullOrWhiteSpace(RayConfiguration.Root["BiliBiliCookie:Bili_jct"]))
            {
                logger.LogWarning(tips, "BILI_JCT", "BILI_JCT");
            }

            return result;
        }


        public override string ToString()
        {
            return $"{GetPropertyDescription(nameof(BiliJct))}={BiliJct};{GetPropertyDescription(nameof(SessData))}={SessData};{GetPropertyDescription(nameof(UserId))}={UserId}";
        }

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }
    }
}
