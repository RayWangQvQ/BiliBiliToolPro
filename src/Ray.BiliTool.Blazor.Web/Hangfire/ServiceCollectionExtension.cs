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
            services.AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSQLiteStorage("bili.db")
                    .UseConsole()
                    .UseDefaultCulture(new CultureInfo("zh-CN"))
                //.UseRecurringJobAdmin(typeof(Startup).Assembly)
            );

            // Add the processing server as IHostedService
            services.AddHangfireServer();
            services.AddHangfireConsoleExtensions();

            return services;
        }
    }
}
