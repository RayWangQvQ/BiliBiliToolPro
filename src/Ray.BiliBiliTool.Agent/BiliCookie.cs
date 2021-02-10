using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Agent
{
    public class BiliCookie : CookieInfo
    {
        private readonly ILogger<BiliCookie> _logger;
        private readonly IConfiguration _configuration;
        private readonly BiliBiliCookieOptions _options;

        public BiliCookie(ILogger<BiliCookie> logger,
            IOptionsMonitor<BiliBiliCookieOptions> optionsMonitor,
            IConfiguration configuration) : base(optionsMonitor.CurrentValue.CookieStr)
        {
            _logger = logger;
            _configuration = configuration;
            _options = optionsMonitor.CurrentValue;

            CookieStr = _options.CookieStr ?? "";
            UserId = _options.UserId ?? "";
            BiliJct = _options.BiliJct ?? "";
            SessData = _options.SessData ?? "";
            OtherCookies = _options.OtherCookies ?? "";

            if (_options.DedeUserID.IsNotNullOrEmpty()) UserId = _options.DedeUserID;
            if (_options.Bili_jct.IsNotNullOrEmpty()) BiliJct = _options.Bili_jct;

            if (CookieStr.IsNotNullOrEmpty())
            {
                foreach (var str in CookieStr.Split(';'))
                {
                    if (str.IsNullOrEmpty()) continue;
                    var list = str.Split('=').ToList();
                    if (list.Count >= 2)
                        CookieDictionary[list[0].Trim()] = list[1].Trim();
                }
            }

            if (CookieDictionary.TryGetValue(GetPropertyDescription(nameof(UserId)), out string userId))
            {
                UserId = userId;
            }
            if (CookieDictionary.TryGetValue(GetPropertyDescription(nameof(BiliJct)), out string jct))
            {
                BiliJct = jct;
            }
            if (CookieDictionary.TryGetValue(GetPropertyDescription(nameof(SessData)), out string sess))
            {
                SessData = sess;
            }

            Check();
        }

        [Description("DedeUserID")]
        public string UserId { get; set; }

        /// <summary>
        /// SESSDATA
        /// </summary>
        [Description("SESSDATA")]
        public string SessData { get; set; }

        [Description("bili_jct")]
        public string BiliJct { get; set; }

        /// <summary>
        /// 其他Cookies
        /// </summary>
        public string OtherCookies { get; set; } = "";

        /// <summary>
        /// 检查是否已配置
        /// </summary>
        /// <returns></returns>
        public void Check()
        {
            bool result = true;
            string msg = "配置项[{0}]为空，该项为必须配置，对应浏览器中Cookie中的[{1}]值";
            string tips = "检测到已配置了[{0}]，已兼容使用[{1}]\r\n";

            //UserId为空，抛异常
            if (string.IsNullOrWhiteSpace(UserId))
            {
                _logger.LogWarning(msg, nameof(UserId), GetPropertyDescription(nameof(UserId)));

                result = false;
            }
            else if (!long.TryParse(UserId, out long uid))//不为空，但不能转换为long，警告
            {
                _logger.LogWarning("UserId：{uid} 不能转换为long型，请确认配置的是正确的Cookie值", UserId);
            }
            //UserId为空，但DedeUserID有值，兼容使用
            if (string.IsNullOrWhiteSpace(_configuration["BiliBiliCookie:UserID"])
                && !string.IsNullOrWhiteSpace(_configuration["BiliBiliCookie:DedeUserID"]))
            {
                _logger.LogWarning(tips, "DEDEUSERID", "DEDEUSERID");
            }

            //SessData为空，抛异常
            if (string.IsNullOrWhiteSpace(SessData))
            {
                _logger.LogWarning(msg, nameof(SessData), GetPropertyDescription(nameof(SessData)));
                result = false;
            }

            //BiliJct为空，抛异常
            if (string.IsNullOrWhiteSpace(BiliJct))
            {
                _logger.LogWarning(msg, nameof(BiliJct), GetPropertyDescription(nameof(BiliJct)));
                result = false;
            }
            //BiliJct为空，但Bili_jct有值，兼容使用
            else if (string.IsNullOrWhiteSpace(_configuration["BiliBiliCookie:BiliJct"])
                && !string.IsNullOrWhiteSpace(_configuration["BiliBiliCookie:Bili_jct"]))
            {
                _logger.LogWarning(tips, "BILI_JCT", "BILI_JCT");
            }

            if (!result)
                throw new Exception($"请正确配置Cookie后再运行，配置方式见 {Constants.SourceCodeUrl}");
        }

        public override string ToString()
        {
            if (CookieStr.IsNotNullOrEmpty()) return CookieStr;

            string re = (OtherCookies ?? "").Trim();
            if (!re.EndsWith(";")) re += ";";

            if (UserId.IsNotNullOrEmpty()) re += $" {GetPropertyDescription(nameof(UserId))}={UserId};";
            if (SessData.IsNotNullOrEmpty()) re += $" {GetPropertyDescription(nameof(SessData))}={SessData};";
            if (BiliJct.IsNotNullOrEmpty()) re += $" {GetPropertyDescription(nameof(BiliJct))}={BiliJct};";

            return re;
        }

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }
    }
}
