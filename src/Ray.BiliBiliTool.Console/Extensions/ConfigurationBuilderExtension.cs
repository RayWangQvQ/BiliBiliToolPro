using System;

#nullable enable

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationBuilderExtension
    {
        public static IConfigurationBuilder AddJsonFileByEnv(
            this IConfigurationBuilder configurationBuilder,
            string envName = "ASPNETCORE_ENVIRONMENT",
            string envprefix = "")
        {
            envName = $"{envprefix}{envName}";
            string? env = Environment.GetEnvironmentVariable(envName);

            configurationBuilder
                .AddJsonFile($"appsettings.{env}.json", true, false);

            return configurationBuilder;
        }
    }
}