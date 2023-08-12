using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;

namespace Ray.BiliTool.Blazor.Web.Auth
{
    public static class AuthenticationHelper
    {
        public static IServiceCollection AddBiliAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authenticationBuilder = services.AddAuthentication();
            if (!string.IsNullOrWhiteSpace(configuration["OAuth:GitHub:ClientId"]))
            {
                authenticationBuilder.AddGitHub(o =>
                {
                    configuration.Bind("OAuth:GitHub", o);
                });
            }

            return services;
        }
    }
}
