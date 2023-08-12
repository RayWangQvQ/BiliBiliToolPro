using Microsoft.Extensions.DependencyInjection;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliTool.Blazor.Web.Auth
{
    public static class AuthorizationHelper
    {
        public const string RequireAdminPolicy = "RequireAdmin";
        public const string RequireManagerPolicy = "RequireManager";
        public const string RequireGuestPolicy = "RequireGuest";

        public static IServiceCollection AddBiliAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                //Policy to be applied to hangfire endpoint
                options.AddPolicy(RequireAdminPolicy, builder =>
                {
                    builder
                        //.AddAuthenticationSchemes(AzureADDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .RequireRole(new[] {RoleType.Admin.ToString()});
                });

                options.AddPolicy(RequireManagerPolicy, builder =>
                {
                    builder
                        .RequireAuthenticatedUser()
                        .RequireRole(new[] { RoleType.Admin.ToString(), RoleType.Manager.ToString() });
                });

                options.AddPolicy(RequireGuestPolicy, builder =>
                {
                    builder
                        .RequireAuthenticatedUser()
                        .RequireRole(new[] { RoleType.Admin.ToString(), RoleType.Manager.ToString(), RoleType.Guest.ToString() });
                });
            });

            return services;
        }
    }
}
