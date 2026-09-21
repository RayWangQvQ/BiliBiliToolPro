using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.DomainService.Interfaces;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// 单项补做：把「某个检查项」映射到具体的执行动作，只做那一项，不重跑整个任务。
/// 各底层动作内部本来就带幂等判断（投币先查今日已投、观看先查是否看过），重复调用是安全的。
/// </summary>
public class TaskRecoveryExecutor(
    CookieStrFactory<BiliCookie> cookieStrFactory,
    IConfiguration configuration,
    IAccountDomainService accountDomainService,
    IVideoDomainService videoDomainService,
    IDonateCoinDomainService donateCoinDomainService,
    IVipPrivilegeDomainService vipPrivilegeDomainService,
    IServiceProvider serviceProvider,
    ILogger<TaskRecoveryExecutor> logger
)
{
    /// <summary>
    /// 执行某一项补做。找不到账号或该项不支持补做时抛异常。
    /// </summary>
    public async Task ExecuteAsync(
        long userId,
        TaskDefinition task,
        TaskItemDefinition item,
        CancellationToken cancellationToken = default
    )
    {
        var ck = FindCookie(userId) ?? throw new Exception($"未找到 UID 为 {userId} 的账号");

        if (item.ItemKey is null)
        {
            await ExecuteWholeTaskAsync(userId, task, cancellationToken);
            return;
        }

        switch (item.ItemKey)
        {
            case "Login":
                await accountDomainService.LoginByCookie(ck);
                break;

            case "Watch":
            {
                var video = await videoDomainService.GetRandomVideoForWatchAndShare(ck);
                await videoDomainService.WatchVideo(video, ck);
                break;
            }

            case TaskCatalog.ShareItemKey:
            {
                var video = await videoDomainService.GetRandomVideoForWatchAndShare(ck);
                try
                {
                    await videoDomainService.OpenVideo(video, ck);
                }
                catch (Exception ex)
                {
                    // 打开失败不阻断分享，与 WatchAndShareVideo 里既有行为保持一致
                    logger.LogWarning(ex, "补做分享前打开视频失败");
                }

                await videoDomainService.ShareVideo(video, ck);
                break;
            }

            case "DonateCoin":
                if (configuration.GetValue("DailyTaskConfig:IsDonateCoinForArticle", false))
                {
                    logger.LogInformation("已开启专栏投币，补做走视频投币分支");
                }

                await donateCoinDomainService.AddCoinsForVideos(ck);
                break;

            case "VipPrivilege":
            {
                var userInfo = await accountDomainService.LoginByCookie(ck);
                await vipPrivilegeDomainService.ReceiveVipPrivilege(userInfo, ck);
                break;
            }

            default:
                throw new Exception($"未知的检查项：{item.ItemKey}");
        }
    }

    private async Task ExecuteWholeTaskAsync(
        long userId,
        TaskDefinition task,
        CancellationToken cancellationToken
    )
    {
        var appService = serviceProvider
            .GetServices<IAccountTaskAppService>()
            .FirstOrDefault(s => s.TaskKey == task.TaskKey);

        if (appService is null)
        {
            throw new Exception($"未注册任务服务：{task.TaskKey}");
        }

        await appService.DoTaskForAccountAsync(userId, cancellationToken);
    }

    private BiliCookie? FindCookie(long userId)
    {
        for (int i = 0; i < cookieStrFactory.Count; i++)
        {
            var ck = cookieStrFactory.GetCookie(i);
            if (ck.UserId == userId.ToString())
            {
                return ck;
            }
        }

        return null;
    }
}
