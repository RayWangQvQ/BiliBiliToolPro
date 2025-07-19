using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliBiliTool.Config.Extensions;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册配置
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBiliBiliConfigs(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        //Options
        services
            .AddOptions()
            .Configure<JsonSerializerOptions>(o => o = JsonSerializerOptionsBuilder.DefaultOptions)
            .Configure<BiliBiliCookieOptions>(configuration.GetSection("BiliBiliCookie"))
            .Configure<DailyTaskOptions>(configuration.GetSection("DailyTaskConfig"))
            .Configure<MangaTaskOptions>(configuration.GetSection("MangaTaskConfig"))
            .Configure<MangaPrivilegeTaskOptions>(
                configuration.GetSection("MangaPrivilegeTaskConfig")
            )
            .Configure<Silver2CoinTaskOptions>(configuration.GetSection("Silver2CoinTaskConfig"))
            .Configure<ChargeTaskOptions>(configuration.GetSection("ChargeTaskConfig"))
            .Configure<LiveLotteryTaskOptions>(configuration.GetSection("LiveLotteryTaskConfig"))
            .Configure<UnfollowBatchedTaskOptions>(
                configuration.GetSection("UnfollowBatchedTaskConfig")
            )
            .Configure<VipBigPointOptions>(configuration.GetSection("VipBigPointConfig"))
            .Configure<SecurityOptions>(configuration.GetSection("Security"))
            .Configure<VipPrivilegeOptions>(configuration.GetSection("VipPrivilegeConfig"))
            .Configure<LiveFansMedalTaskOptions>(
                configuration.GetSection("LiveFansMedalTaskConfig")
            )
            .Configure<QingLongOptions>(configuration.GetSection("QingLongConfig"));

        return services;
    }
}
