using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ray.BiliBiliTool.Config;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Agent
{
    public class BiliCookie : CookieInfo
    {
        private readonly ILogger<BiliCookie> _logger;

        public BiliCookie(string ckStr)
            : this(new List<string> { ckStr }) { }

        public BiliCookie(List<string> ckStrList)
            : this(NullLogger<BiliCookie>.Instance, new CookieStrFactory(ckStrList)) { }

        public BiliCookie(ILogger<BiliCookie> logger, CookieStrFactory cookieStrFactory)
            : base(cookieStrFactory)
        {
            _logger = logger;
        }

        public override string CkValueBuild(string value)
        {
            value = base.CkValueBuild(value);

            if (value.Contains(','))
            {
                value = Uri.EscapeDataString(value);
            }

            return value;
        }

        [Description("DedeUserID")]
        public string UserId => CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(UserId)), out string userId) ? userId : "";

        /// <summary>
        /// SESSDATA
        /// </summary>
        [Description("SESSDATA")]
        public string SessData => CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(SessData)), out string sess) ? sess : "";

        [Description("bili_jct")]
        public string BiliJct => CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(BiliJct)), out string jct) ? jct : "";

        [Description("LIVE_BUVID")]
        public string LiveBuvid => CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(LiveBuvid)), out string liveBuvid) ? liveBuvid : "";

        [Description("buvid3")]
        public string Buvid => CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(Buvid)), out string buvid) ? buvid : "";

        /// <summary>
        /// 检查是否已配置
        /// </summary>
        /// <returns></returns>
        public override void Check()
        {
            base.Check();

            if (CookieItemDictionary.Count == 0) throw new Exception("Cookie字符串格式异常，内部无等号");

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

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }
    }
}
