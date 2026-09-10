using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Host.IntegrationTests.Support;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.Admin;
using Ray.BiliBiliTool.Web.Services.Pages.Login;
using Ray.BiliBiliTool.Web.Services.Pages.Schedules;

namespace Ray.BiliBiliTool.Host.IntegrationTests;

public class WebStartupIntegrationTests
{
    [Fact]
    public Task Web_startup_boots_and_exposes_critical_services()
    {
        using var factory = new WebHostFactory();
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<ILoginTaskAppService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IDailyTaskAppService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ISchedulerFactory>().Should().NotBeNull();
        scope
            .ServiceProvider.GetRequiredService<IOptionsMonitor<DailyTaskOptions>>()
            .Should()
            .NotBeNull();
        scope.ServiceProvider.GetRequiredService<DbInitializer>().Should().NotBeNull();

        var dbContext = scope.ServiceProvider.GetRequiredService<BiliDbContext>();
        dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");

        scope.ServiceProvider.GetRequiredService<IAuthService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ILoginPageStateFactory>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IAdminPageWorkflow>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ISchedulerPageWorkflow>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ILogsDialogWorkflow>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IHistoryDialogWorkflow>().Should().NotBeNull();

        return Task.CompletedTask;
    }
}
