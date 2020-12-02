using System;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Config.Extensions
{
    public static class ConfigurationBuilderExtension
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddExcludeEmptyEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new EnvironmentVariablesExcludeEmptyConfigurationSource());
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables
        /// with a specified prefix.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="prefix">The prefix that environment variable names must start with. The prefix will be removed from the environment variable names.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddExcludeEmptyEnvironmentVariables(
            this IConfigurationBuilder configurationBuilder,
            string prefix)
        {
            configurationBuilder.Add(new EnvironmentVariablesExcludeEmptyConfigurationSource { Prefix = prefix });
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddExcludeEmptyEnvironmentVariables(this IConfigurationBuilder builder, Action<EnvironmentVariablesExcludeEmptyConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}