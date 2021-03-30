using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog.Debugging;

namespace Ray.Serilog.Sinks.Batched
{
    public abstract class PushService
    {
        /// <summary>
        /// 推送端/推送平台名称
        /// </summary>
        public abstract string ClientName { get; }

        public string Msg { get; set; }

        public string Title { get; set; }

        public virtual HttpResponseMessage PushMessage(string message, string title = "")
        {
            this.Msg = message;
            this.Title = title;

            SelfLog.WriteLine($"开始推送到:{ClientName}");

            this.Msg = BuildMsg();

            return DoSend();
        }

        /// <summary>
        /// 构建消息
        /// </summary>
        /// <returns></returns>
        public virtual string BuildMsg()
        {
            return this.Msg;
        }

        /// <summary>
        /// 实际发送
        /// </summary>
        /// <returns></returns>
        public abstract HttpResponseMessage DoSend();
    }
}
