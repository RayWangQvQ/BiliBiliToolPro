using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Ray.BiliBiliTool.Config.Options
{
    /// <summary>
    /// 安全相关配置
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// 是否跳过执行任务，用于特殊情况下，通过配置灵活的开启和关闭任务
        /// </summary>
        public bool IsSkipDailyTask { get; set; } = false;

        /// <summary>
        /// 两次调用api之间间隔的秒数[0,+]
        /// 有人担心在几秒内连续调用api会被b站安全机制发现，所以为不放心的朋友添加了间隔秒数配置，两次调用Api之间会大于该秒数
        /// </summary>
        public int IntervalSecondsBetweenRequestApi { get; set; } = 3;

        /// <summary>
        /// 间隔秒数所针对的HttpMethod，多个用英文逗号隔开，当前有GET和POST两种，可配置如“GET,POST”
        /// 服务器一般对GET请求不是很敏感，建议只针对POST请求做间隔就可以了
        /// </summary>
        public string IntervalMethodTypes { get; set; } = "POST";

        public List<HttpMethod> GetIntervalMethods()
        {
            List<HttpMethod> result = new List<HttpMethod>();
            if (string.IsNullOrWhiteSpace(IntervalMethodTypes)) return result;

            foreach (var item in IntervalMethodTypes.Split(','))
            {
                try
                {
                    HttpMethod method = new HttpMethod(item);
                    if (method != null && !result.Contains(method)) result.Add(method);
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            return result;
        }

        /// <summary>
        /// 请求B站接口时头部传递的User-Agent
        /// </summary>
        public string UserAgent { get; set; } = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.66 Safari/537.36 Edg/87.0.664.41";
    }
}
