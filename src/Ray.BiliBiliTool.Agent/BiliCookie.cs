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

        public BiliCookie(ILogger<BiliCookie> logger,
            CookieStrFactory cookieStrFactory)
            : base(cookieStrFactory.GetCurrentCookieStr())
        {
            _logger = logger;

            if (CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(UserId)), out string userId))
            {
                UserId = userId;
            }
            if (CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(BiliJct)), out string jct))
            {
                BiliJct = jct;
            }
            if (CookieItemDictionary.TryGetValue(GetPropertyDescription(nameof(SessData)), out string sess))
            {
                SessData = sess;
            }

            this.Check();
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

        public override string ToString()
        {
            if (CookieStr.IsNotNullOrEmpty()) return CookieStr;

            return "";
        }

        private string GetPropertyDescription(string propertyName)
        {
            return GetType().GetPropertyDescription(propertyName);
        }
    }
}
