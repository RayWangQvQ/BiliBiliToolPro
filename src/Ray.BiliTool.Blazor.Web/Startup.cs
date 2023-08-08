using System;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Ray.BiliTool.Blazor.Web.Areas.Identity;
using Ray.BiliTool.Blazor.Web.Services;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Storage.SQLite;
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Application.Contracts;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliTool.Repository;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliTool.Repository.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Hangfire.Dashboard;

namespace Ray.BiliTool.Blazor.Web
{
    public class Startup
    {
        private const string HangfirePolicyName = "HangfirePolicy";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddAntDesign();

            services.AddScoped(sp => new HttpClient
            {
                //BaseAddress = new Uri(sp.GetService<NavigationManager>().BaseUri)
                //BaseAddress = new Uri(Configuration["ASPNETCORE_URLS"])
            });

            services.Configure<ProSettings>(Configuration.GetSection("ProSettings"));

            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IDbConfigService, DbConfigService>();

            //EF
            services.AddRepositoryModule(Configuration);
            services.AddDatabaseDeveloperPageExceptionFilter();

            //Identity
            services.AddDefaultIdentity<IdentityUser>(option =>
                {
                    option.SignIn.RequireConfirmedAccount = false;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<BiliDbContext>();

            //Authentication
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
            var authenticationBuilder = services.AddAuthentication();
            if (!string.IsNullOrWhiteSpace(Configuration["OAuth:GitHub:ClientId"]))
            {
                authenticationBuilder.AddGitHub(o =>
                {
                    Configuration.Bind("OAuth:GitHub", o);
                });
            }

            //Authorization
            services.AddAuthorization(options =>
            {
                //Policy to be applied to hangfire endpoint
                options.AddPolicy(HangfirePolicyName, builder =>
                {
                    builder
                        //.AddAuthenticationSchemes(AzureADDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(new[] { RoleType.Admin.ToString(), RoleType.Manager.ToString() });
                });
            });

            //Hangfire
            services.AddHangfire(Configuration);

            services.AddBiliBiliConfigs(Configuration)
                .AddBiliBiliClientApi(Configuration)
                .AddDomainServices()
                .AddAppServices();
        }

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
                //app.UseHsts(); //// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions());

            var supportedCultures = new[]
            {
                new CultureInfo("zh-CN"),
                new CultureInfo("en-US"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh-CN"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapHangfireDashboard("/hangfire", new DashboardOptions()
                {
                    DashboardTitle = "Dashboard",
                    AppPath = "",
                    StatsPollingInterval = 60 * 1000,

                    Authorization = new[] { new MyAuthorizationFilter() }
                })
                    .RequireAuthorization(HangfirePolicyName) //https://sahansera.dev/securing-hangfire-dashboard-with-endpoint-routing-auth-policy-aspnetcore/
                    ;
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
                .UseDefaultCulture(new CultureInfo("zh-CN"))
            //.UseRecurringJobAdmin(typeof(Startup).Assembly)
            );

            // Add the processing server as IHostedService
            services.AddHangfireServer();
            services.AddHangfireConsoleExtensions();

            return services;
        }
    }

    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow all authenticated users to see the Dashboard (potentially dangerous).
            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
