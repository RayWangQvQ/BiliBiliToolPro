using BlazingQuartz;
using BlazingQuartz.Core;
using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using Quartz;
using Quartz.Impl.AdoJobStore;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.Config.SQLite;
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
    var sqliteConnStr = builder.Configuration.GetConnectionString("Sqlite");
    if (!string.IsNullOrEmpty(sqliteConnStr))
    {
        builder.Configuration.AddSqlite(
            connectionString: sqliteConnStr,
            tableName: Ray.BiliBiliTool.Config.Constants.SqliteTableName,
            keyColumnName: "Key",
            valueColumnName: "Value"
        );
    }

    builder
        .Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "BiliBiliToolPro API",
                Version = "v1",
                Description = "BiliBiliToolPro的API接口文档",
                Contact = new OpenApiContact
                {
                    Name = "BiliBiliToolPro",
                    Url = new Uri("https://github.com/RayWangQvQ/BiliBiliToolPro"),
                },
            }
        );
    });

    builder.Services.AddMudServices();

    builder.Services.AddEF();

    builder.Services.AddSerilog(
        (services, lc) =>
            lc
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.SQLite(
                    sqliteDbPath: sqliteConnStr?.Split(';')[0].Split('=')[1],
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
                sqlLiteOptions.ConnectionString =
                    sqliteConnStr ?? throw new InvalidOperationException();
                sqlLiteOptions.TablePrefix = "QRTZ_";
            });
            storeOptions.UseSystemTextJsonSerializer();
        });

        q.AddBiliJobs(builder.Configuration);
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    builder
        .Services.AddWebServices()
        .AddAuthServices()
        .AddAppServices()
        .AddDomainServices()
        .AddBiliBiliConfigs(builder.Configuration)
        .AddBiliBiliClientApi(builder.Configuration);

    var app = builder.Build();

    Global.ServiceProviderRoot = app.Services;

    using var scope = app.Services.CreateScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    dbInitializer.InitializeAsync().Wait();

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

    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(Ray.BiliBiliTool.Web.Client._Imports).Assembly);

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BiliBiliToolPro API V1");
        c.RoutePrefix = "swagger";
    });

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
