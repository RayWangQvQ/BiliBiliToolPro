using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LoginJob(ILogger<LoginJob> logger, ILoginTaskAppService appService)
    : BaseJob<LoginJob>(logger)
{
    private readonly ILogger<LoginJob> _logger = logger;
    public static readonly JobKey Key = new(nameof(LoginJob), Constants.BiliJobGroup);

    protected override async Task DoExecuteAsync(IJobExecutionContext context)
    {
        _logger.LogInformation($"{nameof(LoginJob)} started.");
        await appService.DoTaskAsync();
    }
}
