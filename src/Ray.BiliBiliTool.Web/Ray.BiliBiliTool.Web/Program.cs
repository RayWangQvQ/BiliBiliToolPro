using Quartz;
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

    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
    );

    builder.Services.AddQuartz(q =>
    {
        q.AddJob<TestJob>(opts => opts.WithIdentity(TestJob.Key));

        q.AddTrigger(opts =>
            opts.ForJob(TestJob.Key)
                .WithIdentity("SendEmailJob-trigger")
                //This Cron interval can be described as "run every minute" (when second is zero)
                .WithSimpleSchedule(o => o.WithRepeatCount(5).WithInterval(TimeSpan.FromMinutes(1)))
        );
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    var app = builder.Build();

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
