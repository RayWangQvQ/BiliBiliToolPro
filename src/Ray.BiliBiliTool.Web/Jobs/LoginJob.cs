using Quartz;
using Ray.BiliBiliTool.Application.Contracts;

namespace Ray.BiliBiliTool.Web.Jobs;

public class LoginJob(ILogger<TestJob> logger, ILoginTaskAppService loginTaskAppService) : IJob
{
    public static readonly JobKey Key = new(nameof(LoginJob));

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await DoExecuteAsync(context);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    private async Task DoExecuteAsync(IJobExecutionContext context)
    {
        logger.LogInformation($"{nameof(LoginJob)} started.");
        await loginTaskAppService.DoTaskAsync();
    }
}
