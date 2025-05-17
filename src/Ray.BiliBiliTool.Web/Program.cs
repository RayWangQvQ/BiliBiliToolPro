using BlazingQuartz;
using BlazingQuartz.Core;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Quartz;
using Quartz.Impl.AdoJobStore;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Infrastructure.EF.Extensions;
using Ray.BiliBiliTool.Web.Components;
using Ray.BiliBiliTool.Web.Extensions;
using Serilog;
using Serilog.Debugging;

SelfLog.Enable(Console.Error);
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("config/cookies.json", optional: true, reloadOnChange: true);

    builder
        .Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

    var sqliteConnStr = builder.Configuration.GetConnectionString("Sqlite");

    builder.Services.AddMudServices();

    builder.Services.AddEF();

    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.SQLite(
                    sqliteDbPath: sqliteConnStr.Split(';')[0].Split('=')[1],
                    tableName: "bili_logs",
                    storeTimestampInUtc: true,
                    batchSize: 7
                )
    );

    // Add BlazingQuartz
    builder.Services.Configure<BlazingQuartzUIOptions>(
        builder.Configuration.GetSection("BlazingQuartz")
    );
    builder.Services.AddBlazingQuartz();
    builder.Services.AddMudServices();

    builder.Services.AddQuartz(q =>
    {
        q.UsePersistentStore(storeOptions =>
        {
            storeOptions.UseMicrosoftSQLite(sqlLiteOptions =>
            {
                sqlLiteOptions.UseDriverDelegate<SQLiteDelegate>();
                sqlLiteOptions.ConnectionString = sqliteConnStr;
                sqlLiteOptions.TablePrefix = "QRTZ_";
            });
            storeOptions.UseSystemTextJsonSerializer();
        });

        q.AddBiliJobs(builder.Configuration);
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    builder
        .Services.AddAppServices()
        .AddDomainServices()
        .AddBiliBiliConfigs(builder.Configuration)
        .AddBiliBiliClientApi(builder.Configuration);

    builder.Services.AddAppServices();

    var app = builder.Build();

    Global.ServiceProviderRoot = app.Services;

    using var scope = app.Services.CreateScope();
    var databaseContext = scope.ServiceProvider.GetRequiredService<BiliDbContext>();
    databaseContext.Database.MigrateAsync().Wait();

    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.UseSerilogRequestLogging();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Ray.BiliBiliTool.Web.Client._Imports).Assembly);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
