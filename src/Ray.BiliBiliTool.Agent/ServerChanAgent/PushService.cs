using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Dtos;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent.ServerChanAgent
{
    public class PushService
    {
        public static StringWriter PushStringWriter { get; private set; } = new StringWriter();

        private readonly IPushApi _pushApi;
        private readonly PushOptions _pushOptions;

        public PushService(IPushApi pushApi, IOptionsMonitor<PushOptions> pushOptions)
        {
            _pushApi = pushApi;
            _pushOptions = pushOptions.CurrentValue;
        }

        public PushResponse DoSend(string title, string content)
        {
            return _pushApi.Send(_pushOptions.PushScKey, new PushRequest { Text = title, Desp = content }).Result;
        }

        public PushResponse SendStringWriter()
        {
            if (string.IsNullOrWhiteSpace(_pushOptions.PushScKey)) return new PushResponse { Errno = int.MinValue, Errmsg = "未配置SCKEY" };

            var title = $"Ray.BiliBiliTool任务日报";
            var content = $"#### 日期：{DateTime.Now:yyyy-MM-dd} \r\n{PushStringWriter.GetStringBuilder()}";

            return DoSend(title, content);
        }
    }
}
