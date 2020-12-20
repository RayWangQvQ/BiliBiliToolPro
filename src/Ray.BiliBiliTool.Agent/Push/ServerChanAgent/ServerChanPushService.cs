using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent.Push;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Dtos;
using Ray.BiliBiliTool.Agent.ServerChanAgent.Interfaces;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent.ServerChanAgent
{
    public class ServerChanPushService : IPushService
    {
        private readonly IServerChanPushApi _pushApi;
        private readonly PushOptions _pushOptions;

        public ServerChanPushService(IServerChanPushApi pushApi, IOptionsMonitor<PushOptions> pushOptions)
        {
            _pushApi = pushApi;
            _pushOptions = pushOptions.CurrentValue;
        }

        public string Name => "ServerChan";

        public bool Send(string title, string content)
        {
            var re = _pushApi.Send(_pushOptions.ServerChan.PushScKey, new PushRequest { Text = title, Desp = content }).Result;
            return true;
        }
    }
}
