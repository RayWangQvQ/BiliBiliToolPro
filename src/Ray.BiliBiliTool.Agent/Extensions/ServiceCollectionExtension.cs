using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.BiliBiliAgent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Agent.DaiDai;
using Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Refit;

namespace Ray.BiliBiliTool.Agent.Extensions;

public static class ServiceCollectionExtension
{
    private const int MaxLoggedBodyLength = 2000;

    /// <summary>
    /// 注册强类型api客户端
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBiliBiliClientApi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        //Cookie
        services.AddSingleton<CookieStrFactory<BiliCookie>>();

        //全局代理
        services.SetGlobalProxy(configuration);

        //DelegatingHandler
        services.Scan(scan =>
            scan.FromAssemblyOf<BiliBiliCommonHeadersDelegatingHandler>()
                .AddClasses(classes => classes.AssignableTo<DelegatingHandler>())
                .AsSelf()
                .WithTransientLifetime()
        );

        //服务
        services.AddScoped<IWbiService, WbiService>();

        //bilibli
        Action<IServiceProvider, HttpClient> config = (sp, c) =>
        {
            c.DefaultRequestHeaders.Add(
                "User-Agent",
                sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgent
            );
            c.Timeout = BiliResiliencePolicies.HttpTimeout;
        };
        Action<IServiceProvider, HttpClient> configApp = (sp, c) =>
        {
            c.DefaultRequestHeaders.Add(
                "User-Agent",
                sp.GetRequiredService<IOptionsMonitor<SecurityOptions>>().CurrentValue.UserAgentApp
            );
            c.Timeout = BiliResiliencePolicies.HttpTimeout;
        };

        services.AddBiliBiliClientApi<INavApi>(BiliHosts.Api, config, true);

        services.AddBiliBiliClientApi<IApiApi>(
            BiliHosts.Api,
            config,
            policy: BiliResiliencePolicies.MutatingPolicy()
        );

        services.AddBiliBiliClientApi<IShowApi>(BiliHosts.Show, config);
        services.AddBiliBiliClientApi<IPassportApi>(BiliHosts.Passport, config);
        services.AddBiliBiliClientApi<ILiveTraceApi>(BiliHosts.LiveTrace, config);
        services.AddBiliBiliClientApi<IHomeApi>(BiliHosts.Www, config);
        services.AddBiliBiliClientApi<IMangaApi>(BiliHosts.Manga, config);
        services.AddBiliBiliClientApi<IAccountApi>(BiliHosts.Account, config);
        services.AddBiliBiliClientApi<ILiveApi>(
            BiliHosts.Live,
            config,
            policy: BiliResiliencePolicies.MutatingPolicy()
        );

        //qinglong
        var qinglongHost = configuration["QL_URL"] ?? "http://localhost:5600";
        services
            .AddRefitClient<IQingLongApi>()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    c.BaseAddress = new Uri(qinglongHost);
                    c.DefaultRequestHeaders.Add(
                        "User-Agent",
                        sp.GetRequiredService<
                            IOptionsMonitor<SecurityOptions>
                        >().CurrentValue.UserAgent
                    );
                    c.Timeout = BiliResiliencePolicies.HttpTimeout;
                }
            )
            .AddPolicyHandler(BiliResiliencePolicies.ReadOnlyPolicy());

        //baihu
        var baihuHost = configuration["BA_URL"] ?? "http://localhost:8052";
        services
            .AddRefitClient<IBaihuApi>()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    c.BaseAddress = new Uri(baihuHost);
                    c.DefaultRequestHeaders.Add(
                        "User-Agent",
                        sp.GetRequiredService<
                            IOptionsMonitor<SecurityOptions>
                        >().CurrentValue.UserAgent
                    );
                }
            )
            .AddPolicyHandler(BiliResiliencePolicies.ReadOnlyPolicy());

        //daidai（呆呆面板原生 Open API）
        var daidaiHost = configuration["DaiDai_URL"] ?? "http://127.0.0.1:5700";
        services
            .AddRefitClient<IDaiDaiApi>()
            .ConfigureHttpClient(
                (sp, c) =>
                {
                    c.BaseAddress = new Uri(daidaiHost);
                    c.DefaultRequestHeaders.Add(
                        "User-Agent",
                        sp.GetRequiredService<
                            IOptionsMonitor<SecurityOptions>
                        >().CurrentValue.UserAgent
                    );
                }
            )
            .AddPolicyHandler(BiliResiliencePolicies.ReadOnlyPolicy());

        return services;
    }

    /// <summary>
    /// 封装Refit，默认将Cookie添加到Header中
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <param name="services"></param>
    /// <param name="host"></param>
    /// <returns></returns>
    private static IServiceCollection AddBiliBiliClientApi<TInterface>(
        this IServiceCollection services,
        string host,
        Action<IServiceProvider, HttpClient> config,
        bool ignorWrid = false,
        IAsyncPolicy<HttpResponseMessage>? policy = null
    )
        where TInterface : class
    {
        IHttpClientBuilder httpClientBuilder = services
            .AddRefitClient<TInterface>(sp => sp.CreateBiliBiliRefitSettings())
            .ConfigureHttpClient((_, c) => c.BaseAddress = new Uri(host))
            .ConfigureHttpClient(config)
            .AddHttpMessageHandler<FormUrlEncodedKeyNormalizingDelegatingHandler>()
            .AddHttpMessageHandler<LogDelegatingHandler>()
            .AddHttpMessageHandler<BiliBiliCommonHeadersDelegatingHandler>()
            .AddHttpMessageHandler<IntervalDelegatingHandler>()
            .AddPolicyHandler(policy ?? BiliResiliencePolicies.ReadOnlyPolicy());

        if (!ignorWrid)
        {
            httpClientBuilder.AddHttpMessageHandler<WridEncryptionDelegatingHandler>();
        }

        return services;
    }

    /// <summary>
    /// 注入 B 站客户端共用的 Refit 配置：
    /// 1) query 参数名首字母小写，与迁移前的线上报文保持一致；
    /// 2) 解析失败时输出可定位的诊断信息。Refit 会把所有响应解析异常压成
    ///    "An error occured deserializing the response."，真实原因只留在 InnerException 中，
    ///    而调用方普遍只打印 e.Message，导致业务错误码（如 -400）无法被发现。
    /// </summary>
    private static RefitSettings CreateBiliBiliRefitSettings(this IServiceProvider serviceProvider)
    {
        ILogger logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ServiceCollectionExtension).FullName!);

        RefitSettings? settings = null;

        settings = new RefitSettings
        {
            UrlParameterKeyFormatter = new BiliUrlParameterKeyFormatter(),
            DeserializationExceptionFactory = async (response, exception) =>
            {
                HttpRequestMessage? request = response.RequestMessage;
                if (request is null)
                {
                    return exception;
                }

                string body = await response.Content.ReadAsStringAsync();
                if (body.Length > MaxLoggedBodyLength)
                {
                    body = body[..MaxLoggedBodyLength] + "...(已截断)";
                }

                string reason = exception.Message.Replace('\r', ' ').Replace('\n', ' ');

                string detail =
                    $"响应解析失败 {request.Method} {Mask(request.RequestUri?.ToString() ?? "")} "
                    + $"| HTTP {(int)response.StatusCode} {response.ReasonPhrase} "
                    + $"| 真实原因({exception.GetType().Name}): {reason} "
                    + $"| 响应体: {Mask(body.Replace('\r', ' ').Replace('\n', ' '))}";

                logger.LogWarning("{detail}", detail);

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug(
                        "实际发出的请求: {request}",
                        await DescribeRequestAsync(request)
                    );
                }

                return await ApiException.Create(
                    detail,
                    request,
                    request.Method,
                    response,
                    settings!,
                    exception
                );
            },
        };

        return settings;
    }

    private static readonly Regex _sensitiveRegex = new(
        @"(?<prefix>[?&; ]|^)(?<key>csrf|bili_jct|SESSDATA|access_key|access_token|refresh_token|app_secret|api_key|password|pwd|token|buvid3|buvid4)(?<rest>=[^&;]*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// 输出实际发出的请求（方法、完整URL、全部请求头、请求体），凭据类值一律掩码。
    /// </summary>
    private static async Task<string> DescribeRequestAsync(HttpRequestMessage request)
    {
        StringBuilder sb = new();
        sb.Append(request.Method).Append(' ').Append(Mask(request.RequestUri?.ToString() ?? ""));

        foreach ((string name, IEnumerable<string> values) in request.Headers)
        {
            string value = string.Join("; ", values);
            if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                // 只保留Cookie名，值全部掩码，避免凭据进入日志与推送
                value = string.Join(
                    "; ",
                    value
                        .Split(
                            ';',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        )
                        .Select(pair => pair.Split('=', 2)[0] + "=***")
                );
            }
            else if (name.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                value = "***";
            }

            sb.Append(" | ").Append(name).Append(": ").Append(Mask(value));
        }

        if (request.Content is not null)
        {
            foreach ((string name, IEnumerable<string> values) in request.Content.Headers)
            {
                sb.Append(" | ").Append(name).Append(": ").Append(Mask(string.Join("; ", values)));
            }

            string content = await request.Content.ReadAsStringAsync();
            if (content.Length > MaxLoggedBodyLength)
            {
                content = content[..MaxLoggedBodyLength] + "...(已截断)";
            }
            sb.Append(" | 请求体: ").Append(Mask(content.Replace('\r', ' ').Replace('\n', ' ')));
        }

        return sb.ToString();
    }

    private static string Mask(string text) =>
        _sensitiveRegex.Replace(
            text,
            m => $"{m.Groups["prefix"].Value}{m.Groups["key"].Value}=***"
        );

    /// <summary>
    /// 设置全局代理(如果配置了代理)
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection SetGlobalProxy(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var proxyAddress = configuration["Security:WebProxy"];
        if (!string.IsNullOrWhiteSpace(proxyAddress))
        {
            WebProxy webProxy = new WebProxy();

            //user:password@host:port http proxy only .Tested with tinyproxy-1.11.0-rc1
            if (proxyAddress!.Contains("@"))
            {
                string userPass = proxyAddress.Split("@")[0];
                string address = proxyAddress.Split("@")[1];

                string proxyUser = "";
                string proxyPass = "";
                if (userPass.Contains(":"))
                {
                    proxyUser = userPass.Split(":")[0];
                    proxyPass = userPass.Split(":")[1];
                }

                webProxy.Address = new Uri("http://" + address);
                webProxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
            }
            else
            {
                webProxy.Address = new Uri(proxyAddress);
            }

            HttpClient.DefaultProxy = webProxy;
        }

        return services;
    }
}
