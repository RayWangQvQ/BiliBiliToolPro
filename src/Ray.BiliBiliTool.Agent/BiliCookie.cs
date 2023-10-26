using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Agent
{
    public class BiliCookie : CookieContainer
    {
        private readonly ILogger<BiliCookie> _logger;

        public BiliCookie()
        {
        }
        public BiliCookie(string cookieHeader)
        {
            this.Init(cookieHeader);
        }

        public void Init(string ckStr)
        {
            var dic= CkStrToDictionary(ckStr);
            foreach (var item in dic)
            {
                this.Add(new Cookie(item.Key, CkValueBuild(item.Value), "/", ".bilibili.com"));
            }
        }

        public string CkValueBuild(string value)
        {
            if (value.Contains(','))
            {
                value = Uri.EscapeDataString(value);
            }

            return value;
        }

        [Description("DedeUserID")]
        public string UserId => this.GetAllCookies().FirstOrDefault(x => x.Name == GetPropertyDescription(nameof(UserId)))?.Value;

        /// <summary>
        /// SESSDATA
        /// </summary>
        [Description("SESSDATA")]
        public string SessData => this.GetAllCookies().FirstOrDefault(x => x.Name == GetPropertyDescription(nameof(SessData)))?.Value;

        [Description("bili_jct")]
        public string BiliJct => this.GetAllCookies().FirstOrDefault(x => x.Name == GetPropertyDescription(nameof(BiliJct)))?.Value;

        [Description("LIVE_BUVID")]
        public string LiveBuvid => this.GetAllCookies().FirstOrDefault(x => x.Name == GetPropertyDescription(nameof(LiveBuvid)))?.Value;

        [Description("buvid3")]
        public string Buvid => this.GetAllCookies().FirstOrDefault(x => x.Name == GetPropertyDescription(nameof(Buvid)))?.Value;

        /// <summary>
        /// 检查是否已配置
        /// </summary>
        /// <returns></returns>
        public void Check()
        {
            if (this.GetAllCookies().Count == 0) throw new Exception("Cookie字符串格式异常，内部无等号");

            bool result = true;
            string msg = "Cookie字符串异常，无[{1}]项";

            //UserId为空，抛异常
            if (string.IsNullOrWhiteSpace(UserId))
            {
                _logger.LogWarning(msg, GetPropertyDescription(nameof(UserId)));

                result = false;
            }
            else if (!long.TryParse(UserId, out long uid))//不为空，但不能转换为long，警告
            {
                _logger.LogWarning("[{uidKey}]={uid} 不能转换为long型，请确认配置的是正确的Cookie值", GetPropertyDescription(nameof(UserId)), UserId);
            }

            //SessData为空，抛异常
            if (string.IsNullOrWhiteSpace(SessData))
            {
                _logger.LogWarning(msg, GetPropertyDescription(nameof(SessData)));
                result = false;
            }

            //BiliJct为空，抛异常
            if (string.IsNullOrWhiteSpace(BiliJct))
            {
                _logger.LogWarning(msg, GetPropertyDescription(nameof(BiliJct)));
                result = false;
            }

            if (!result)
                throw new Exception($"请正确配置Cookie后再运行，配置方式见 {Constants.SourceCodeUrl}");
        }

        public string GetCookieStr()
        {
            var cookieStr = this.GetCookieHeader(new Uri("https://bilibili.com"));
            return cookieStr;
        }

        #region  private

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }

        private Dictionary<string, string> CkStrToDictionary(string ckStr)
        {
            var dic = new Dictionary<string, string>();
            var ckItemList = ckStr
                .Split(";", StringSplitOptions.TrimEntries)
                .Distinct()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            foreach (var item in ckItemList)
            {
                var key = item[..item.IndexOf("=", StringComparison.Ordinal)].Trim();
                var value = item[(item.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim();
                dic.AddIfNotExist(new KeyValuePair<string, string>(key, value), p => p.Key == key);
            }
            return dic;
        }

        #endregion

    }
}
