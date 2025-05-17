using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;

public class IntervalDelegatingHandler(IOptionsMonitor<SecurityOptions> securityOptions)
    : DelegatingHandler
{
    private readonly Dictionary<string, int> _special = new()
    {
        { "/xlive/lottery-interface/v1/Anchor/Join", 3 }, //天选抽奖，有时效，不能间隔过久，使用默认3秒
        { "/xlive/data-interface/v1/x25Kn/E", 1 },
        { "/xlive/data-interface/v1/x25Kn/X", 1 },
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        await IntervalForSecurityAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task IntervalForSecurityAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (securityOptions.CurrentValue.IntervalSecondsBetweenRequestApi <= 0)
            return;
        if (!securityOptions.CurrentValue.GetIntervalMethods().Contains(request.Method))
            return;

        int seconds = 0;
        //需要特殊处理的接口
        if (_special.TryGetValue(request.RequestUri.AbsolutePath, out int s))
        {
            seconds = s;
        }
        else
        {
            int maxSeconds = securityOptions.CurrentValue.IntervalSecondsBetweenRequestApi;
            seconds = new Random().Next(maxSeconds / 2, maxSeconds + 1);
        }

        await Task.Delay(seconds * 1000, cancellationToken);
    }
}
