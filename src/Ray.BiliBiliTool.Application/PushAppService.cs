using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Push;
using Ray.BiliBiliTool.Application.Attributes;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Application
{
    public class PushAppService : IPushAppService
    {
        private readonly ILogger<PushAppService> _logger;
        private readonly IEnumerable<IPushService> _pushServices;
        private readonly PushOptions _pushOptions;

        public PushAppService(ILogger<PushAppService> logger,
            IEnumerable<IPushService> pushServices,
            IOptionsMonitor<PushOptions> pushOptions)
        {
            _logger = logger;
            _pushServices = pushServices;
            _pushOptions = pushOptions.CurrentValue;
        }

        public void Push()
        {
            if (_pushOptions.Strategy.IsNullOrEmpty()) return;

            _logger.LogInformation("开始推送");

            var title = $"Ray.BiliBiliTool任务日报";
            var content = $"#### 日期：{DateTime.Now:yyyy-MM-dd} \r\n{Global.PushStringWriter.GetStringBuilder()}";

            var pushService = _pushServices.FirstOrDefault(it => it.Name == _pushOptions.Strategy);
            if (pushService == null)
            {
                _logger.LogError("推送策略：{name} 不存在", _pushOptions.Strategy);
                throw new Exception($"推送策略：{_pushOptions.Strategy} 不存在");
            }

            pushService.Send(title, content);//todo：封装返回类型
        }
    }
}
