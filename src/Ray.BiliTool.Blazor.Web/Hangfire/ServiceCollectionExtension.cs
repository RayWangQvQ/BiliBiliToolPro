using System.Globalization;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Storage.SQLite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ray.BiliTool.Blazor.Web.Hangfire
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration config)
        {
            // Add Hangfire services.
            services.AddSingleton(new AutomaticRetryAttribute { Attempts = 0 });
            services.AddHangfire((sp, configuration) => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSQLiteStorage("db/bili.db")
                    .UseConsole()
                    .UseDefaultCulture(new CultureInfo("zh-CN"))
                    .UseFilter(sp.GetRequiredService<AutomaticRetryAttribute>())
            //.UseRecurringJobAdmin(typeof(Startup).Assembly)
            );

            // Add the processing server as IHostedService
            services.AddHangfireServer();
            services.AddHangfireConsoleExtensions();

            return services;
        }
    }
}
