using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ray.BiliTool.Blazor.Web.Areas.Identity;
using Ray.BiliTool.Blazor.Web.Services;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.RecurringJobAdmin;
using Hangfire.Storage.SQLite;
using Microsoft.AspNetCore.Identity.UI.Services;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Ray.Repository.EntityFramework;
using Ray.BiliTool.Repository;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliTool.Repository.Extensions;
using Serilog.Core;

namespace Ray.BiliTool.Blazor.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddAntDesign();

            services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(sp.GetService<NavigationManager>().BaseUri)
            });

            services.Configure<ProSettings>(Configuration.GetSection("ProSettings"));

            services.AddScoped<IChartService, ChartService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IDbConfigService, DbConfigService>();

            // EF
            services.AddRepositoryModule(Configuration);
            services.AddDatabaseDeveloperPageExceptionFilter();

            // Identity
            services.AddDefaultIdentity<IdentityUser>(option =>
                {
                    option.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<BiliDbContext>();

            // Authentication
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            services.AddAuthentication()
                .AddGitHub(o =>
                {
                    Configuration.Bind("OAuth:GitHub", o);
                })
                ;

            //Hangfire
            services.AddHangfire(Configuration);

            services.AddBiliBiliConfigs(Configuration)
                .AddBiliBiliClientApi(Configuration)
                .AddDomainServices()
                .AddAppServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Global.ServiceProviderRoot = app.ApplicationServices;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint(); // Migrations
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapHangfireDashboard();
            });

            using var scope = app.ApplicationServices.CreateScope();
            InitHangfireJobs(scope.ServiceProvider);
            scope.ServiceProvider.Seed();
        }

        private static void InitHangfireJobs(IServiceProvider sp)
        {

            BackgroundJob.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            //Test
            RecurringJob.AddOrUpdate<ITestAppService>("Test",
                x => x.DoTaskAsync(new CancellationToken()),
                Cron.Daily);

            //Daily
            var dailyOptions = sp.GetRequiredService<IOptions<DailyTaskOptions>>().Value;
            string dailyCode = typeof(IDailyTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<IDailyTaskAppService>(dailyCode,
                x => x.DoTaskAsync(new CancellationToken()),
                dailyOptions.Cron);

            //LiveLottery
            var liveLotteryOptions = sp.GetRequiredService<IOptions<LiveLotteryTaskOptions>>().Value;
            string liveLotteryCode = typeof(ILiveLotteryTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<ILiveLotteryTaskAppService>(liveLotteryCode,
                x => x.DoTaskAsync(new CancellationToken()),
                liveLotteryOptions.Cron);

            //LiveFansMedal
            var liveFansMedalOptions = sp.GetRequiredService<IOptions<LiveFansMedalTaskOptions>>().Value;
            string liveFansMedalCode = typeof(ILiveFansMedalAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<ILiveFansMedalAppService>(liveFansMedalCode,
                x => x.DoTaskAsync(new CancellationToken()),
                liveFansMedalOptions.Cron);

            //UnfollowBatched
            var unfollowBatchedOptions = sp.GetRequiredService<IOptions<UnfollowBatchedTaskOptions>>().Value;
            string unfollowBatchedCode = typeof(IUnfollowBatchedTaskAppService).GetCustomAttribute<DescriptionAttribute>()?.Description;
            RecurringJob.AddOrUpdate<IUnfollowBatchedTaskAppService>(unfollowBatchedCode,
                x => x.DoTaskAsync(new CancellationToken()),
                unfollowBatchedOptions.Cron);
        }
    }

    public static class ServiceCollectionExt
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
            //.UseRecurringJobAdmin(typeof(Startup).Assembly)
            );

            // Add the processing server as IHostedService
            services.AddHangfireServer();
            services.AddHangfireConsoleExtensions();

            return services;
        }
    }
}
