using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Ray.BiliBiliTool.Web.Extensions;

public static class ForwardedHeadersConfigurationExtensions
{
    public static IServiceCollection AddBiliForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (
                var value in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()
                    ?? []
            )
            {
                if (!IPAddress.TryParse(value, out var address))
                    throw new InvalidOperationException(
                        "ReverseProxy:KnownProxies must contain IP addresses"
                    );
                options.KnownProxies.Add(address);
            }
            foreach (
                var value in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>()
                    ?? []
            )
            {
                if (!System.Net.IPNetwork.TryParse(value, out var network))
                    throw new InvalidOperationException(
                        "ReverseProxy:KnownNetworks must contain CIDR networks"
                    );
                options.KnownIPNetworks.Add(network);
            }
        });
        return services;
    }
}
