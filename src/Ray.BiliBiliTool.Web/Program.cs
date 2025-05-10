using BlazingQuartz;
using BlazingQuartz.Core;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Quartz;
using Quartz.Impl.AdoJobStore;
using Ray.BiliBiliTool.Infrastructure.EF;
using Ray.BiliBiliTool.Infrastructure.EF.Extensions;
using Ray.BiliBiliTool.Web;
using Ray.BiliBiliTool.Web.Client.Pages;
using Ray.BiliBiliTool.Web.Components;
using Ray.BiliBiliTool.Web.Jobs;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

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
                .WriteTo.SQLite(
                    sqliteDbPath: sqliteConnStr.Split(';')[0].Split('=')[1],
                    tableName: "bili_logs",
                    storeTimestampInUtc: true
                )
    );

    builder.Services.AddQuartz(q =>
    {
        q.UsePersistentStore(storeOptions =>
        {
            storeOptions.UseSQLite(sqlLiteOptions =>
            {
                sqlLiteOptions.UseDriverDelegate<SQLiteDelegate>();
                sqlLiteOptions.ConnectionString = sqliteConnStr;
                sqlLiteOptions.TablePrefix = "QRTZ_";
            });
            storeOptions.UseSystemTextJsonSerializer();
        });

        // Test job
        q.AddJob<TestJob>(opts => opts.WithIdentity(TestJob.Key));
        q.AddTrigger(opts =>
            opts.ForJob(TestJob.Key)
                .WithIdentity($"{TestJob.Key}-trigger")
                .WithSimpleSchedule(o => o.WithRepeatCount(3).WithInterval(TimeSpan.FromMinutes(1)))
        );
        q.AddTrigger(opts =>
            opts.ForJob(TestJob.Key)
                .WithIdentity($"{TestJob.Key}-cron-trigger")
                .WithCronSchedule("0 0/5 * * * ?")
        );

        // Login job
        q.AddJob<LoginJob>(opts => opts.WithIdentity(LoginJob.Key));
        q.AddTrigger(opts =>
            opts.ForJob(LoginJob.Key)
                .WithIdentity($"{LoginJob.Key}-cron-trigger")
                .WithCronSchedule("0 0 0 1 1 ?")
        );
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    // Add BlazingQuartz
    builder.Services.Configure<BlazingQuartzUIOptions>(
        builder.Configuration.GetSection("BlazingQuartz")
    );
    builder.Services.AddBlazingQuartz();
    builder.Services.AddMudServices();

    var app = builder.Build();

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
