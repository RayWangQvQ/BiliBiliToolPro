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

        protected virtual string NewLineStr { get; }

        public virtual HttpResponseMessage PushMessage(string message, string title = "")
        {
            this.Msg = message;
            this.Title = title;

            SelfLog.WriteLine($"开始推送到:{ClientName}");

            BuildMsg();

            return DoSend();
        }

        /// <summary>
        /// 构建消息
        /// </summary>
        /// <returns></returns>
        public virtual void BuildMsg()
        {
            //如果指定换行符，则替换；不指定，不替换
            if (!string.IsNullOrEmpty(NewLineStr))
                this.Msg = Msg.Replace(Environment.NewLine, this.NewLineStr);
        }

        /// <summary>
        /// 实际发送
        /// </summary>
        /// <returns></returns>
        public abstract HttpResponseMessage DoSend();
    }
}
