using Quartz;
using Ray.BiliBiliTool.Web.Client.Pages;
using Ray.BiliBiliTool.Web.Components;
using Ray.BiliBiliTool.Web.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddQuartz(q =>
{
    // Just use the name of your job that you created in the Jobs folder.
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Ray.BiliBiliTool.Web.Client._Imports).Assembly);

app.Run();
