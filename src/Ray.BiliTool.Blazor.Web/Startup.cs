using System.Net.Http;
using Microsoft.AspNetCore.Builder;
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
using Ray.BiliBiliTool.Agent.Extensions;
using Ray.BiliBiliTool.Application.Extensions;
using Ray.BiliBiliTool.Config.Extensions;
using Ray.BiliBiliTool.DomainService.Extensions;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliTool.Repository;
using Ray.BiliTool.Repository.Extensions;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Ray.BiliTool.Blazor.Web.Hangfire;

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

            services.AddControllers();
            services.AddSwaggerGen();

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
                app.UseSwagger();
                app.UseSwaggerUI();

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

                    Authorization = new[] { new BiliAuthorizationFilter() }
                })
                    .RequireAuthorization(HangfirePolicyName) //https://sahansera.dev/securing-hangfire-dashboard-with-endpoint-routing-auth-policy-aspnetcore/
                    ;

                endpoints.MapControllers();
            });

            using var scope = app.ApplicationServices.CreateScope();
            HangfireHelper.InitHangfireJobs(scope.ServiceProvider);
            scope.ServiceProvider.Seed();
        }
    }
}
