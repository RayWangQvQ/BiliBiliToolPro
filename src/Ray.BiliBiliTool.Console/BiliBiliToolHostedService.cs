using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure.Cookie;
using Constants = Ray.BiliBiliTool.Config.Constants;

namespace Ray.BiliBiliTool.Console;

public class BiliBiliToolHostedService(
    IHostApplicationLifetime applicationLifetime,
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<BiliBiliToolHostedService> logger,
    CookieStrFactory cookieStrFactory,
    IOptionsMonitor<SecurityOptions> securityOptions
) : IHostedService
{
    private readonly SecurityOptions _securityOptions = securityOptions.CurrentValue;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool isNotifySingle = configuration
            .GetSection("Notification:IsSingleAccountSingleNotify")
            .Get<bool>();

        try
        {
            logger.LogInformation("BiliBiliToolPro 开始运行...{newLine}", Environment.NewLine);

            bool pass = await PreCheckAsync(cancellationToken);
            if (!pass)
                return;

            await RandomSleepAsync(cancellationToken);

            string[] tasks = await ReadTargetTasksAsync(cancellationToken);
            logger.LogInformation("【目标任务】{tasks}", string.Join(",", tasks));

            if (tasks.Contains("Login"))
            {
                await DoTasksAsync(tasks, cancellationToken);
            }
            else
            {
                for (int i = 0; i < cookieStrFactory.Count; i++)
                {
                    cookieStrFactory.CurrentNum = i + 1;
                    logger.LogInformation(
                        "######### 账号 {num} #########{newLine}",
                        cookieStrFactory.CurrentNum,
                        Environment.NewLine
                    );

                    try
                    {
                        await DoTasksAsync(tasks, cancellationToken);
                        if (isNotifySingle)
                        {
                            LogAppInfo();

                            string accountName =
                                cookieStrFactory.Count > 1
                                    ? $"账号【{cookieStrFactory.CurrentNum}】"
                                    : "";
                            logger.LogInformation(
                                "·开始推送·{task}·{user}",
                                $"{configuration["RunTasks"]}任务",
                                accountName
                            );
                        }
                    }
                    catch (Exception e)
                    {
                        //ignore
                        logger.LogWarning("异常：{msg}", e);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("程序异常终止，原因：{msg}", ex.Message);
            throw;
        }
        finally
        {
            if (!isNotifySingle)
            {
                LogAppInfo();
                logger.LogInformation(
                    "·开始推送·{task}·{user}",
                    $"{configuration["RunTasks"]}任务",
                    ""
                );
            }
            //环境
            logger.LogInformation("运行环境：{env}", environment.EnvironmentName);
            logger.LogInformation(
                "应用目录：{path}{newLine}",
                environment.ContentRootPath,
                Environment.NewLine
            );
            logger.LogInformation("运行结束");

            //自动退出
            applicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task<bool> PreCheckAsync(CancellationToken cancellationToken)
    {
        //Cookie
        logger.LogInformation(
            "【账号个数】{count}个{newLine}",
            cookieStrFactory.Count,
            Environment.NewLine
        );

        //是否跳过
        if (_securityOptions.IsSkipDailyTask)
        {
            logger.LogWarning("已配置为跳过任务{newLine}", Environment.NewLine);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private async Task RandomSleepAsync(CancellationToken cancellationToken)
    {
        if (
            configuration["RunTasks"].Contains("Login")
            || configuration["RunTasks"].Contains("Test")
        )
            return;

        if (_securityOptions.RandomSleepMaxMin > 0)
        {
            int randomMin = new Random().Next(1, ++_securityOptions.RandomSleepMaxMin);
            logger.LogInformation("随机休眠{min}分钟{newLine}", randomMin, Environment.NewLine);
            await Task.Delay(randomMin * 1000 * 60, cancellationToken);
        }
    }

    /// <summary>
    /// 读取目标任务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private Task<string[]> ReadTargetTasksAsync(CancellationToken cancellationToken)
    {
        string[] tasks = configuration["RunTasks"]
            .Split("&", options: StringSplitOptions.RemoveEmptyEntries);
        if (tasks.Any())
        {
            return Task.FromResult(tasks);
        }

        logger.LogInformation("未指定目标任务，请选择要运行的任务：");
        TaskTypeFactory.Show(logger);
        logger.LogInformation("请输入：");

        while (true)
        {
            string index = System.Console.ReadLine();
            bool suc = int.TryParse(index, out int num);
            if (suc)
            {
                string code = TaskTypeFactory.GetCodeByIndex(num);
                configuration["RunTasks"] = code;
                return Task.FromResult(new[] { code });
            }

            logger.LogWarning("输入异常，请输入序号");
        }
    }

    private async Task DoTasksAsync(string[] tasks, CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        foreach (string task in tasks)
        {
            Type type = TaskTypeFactory.Get(task);
            if (type == null)
            {
                logger.LogWarning("任务不存在：{task}", task);
                continue;
            }

            IAppService appService = (IAppService)scope.ServiceProvider.GetRequiredService(type);
            await appService?.DoTaskAsync(cancellationToken);
        }
    }

    private void LogAppInfo()
    {
        logger.LogInformation(
            "{newLine}========================{newLine} v{version} 开源 by {url}",
            Environment.NewLine + Environment.NewLine,
            Environment.NewLine + Environment.NewLine,
            typeof(Program)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion,
            Constants.SourceCodeUrl + Environment.NewLine
        );
        //_logger.LogInformation("【当前IP】{ip} ", IpHelper.GetIp());
    }
}
