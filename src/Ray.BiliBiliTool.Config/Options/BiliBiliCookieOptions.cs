using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;

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
                    : Global.ConfigurationRoot["BiliBiliCookie:DedeUserID"];//为了兼容 GitHub Secrets 经常会被填错
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
                    : Global.ConfigurationRoot["BiliBiliCookie:Bili_jct"];//为了兼容 GitHub Secrets 经常会被填错
            set => _biliJct = value;
        }

        public void SetUserId(string userId)
        {
            this.UserId = userId;
            Global.ConfigurationRoot["BiliBiliCookie:UserID"] = userId;
        }

        /// <summary>
        /// 检查是否已配置
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public void Check(ILogger logger)
        {
            bool result = true;
            string msg = "配置项[{0}]为空，该项为必须配置，对应浏览器中Cookie中的[{1}]值";
            string tips = "检测到已配置了[{0}]，已兼容使用[{1}]\r\n";

            //UserId为空，抛异常
            if (string.IsNullOrWhiteSpace(UserId))
            {
                logger.LogWarning(msg, nameof(UserId), GetPropertyDescription(nameof(UserId)));

                result = false;
            }
            else if (!long.TryParse(UserId, out long uid))//不为空，但不能转换为long，警告
            {
                logger.LogWarning("UserId：{uid} 不能转换为long型，请确认配置的是正确的Cookie值", UserId);
            }
            //UserId为空，但DedeUserID有值，兼容使用
            if (string.IsNullOrWhiteSpace(Global.ConfigurationRoot["BiliBiliCookie:UserID"])
                && !string.IsNullOrWhiteSpace(Global.ConfigurationRoot["BiliBiliCookie:DedeUserID"]))
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
            else if (string.IsNullOrWhiteSpace(Global.ConfigurationRoot["BiliBiliCookie:BiliJct"])
                && !string.IsNullOrWhiteSpace(Global.ConfigurationRoot["BiliBiliCookie:Bili_jct"]))
            {
                logger.LogWarning(tips, "BILI_JCT", "BILI_JCT");
            }

            if (!result)
                throw new Exception($"请正确配置Cookie后再运行，配置方式见 {Constants.SourceCodeUrl}");
        }

        public override string ToString()
        {
            string re = "";

            if (UserId.IsNotNullOrEmpty()) re += $"{GetPropertyDescription(nameof(UserId))}={UserId}; ";
            if (SessData.IsNotNullOrEmpty()) re += $"{GetPropertyDescription(nameof(SessData))}={SessData}; ";
            if (BiliJct.IsNotNullOrEmpty()) re += $"{GetPropertyDescription(nameof(BiliJct))}={BiliJct};";

            return re;
        }

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }
    }
}
