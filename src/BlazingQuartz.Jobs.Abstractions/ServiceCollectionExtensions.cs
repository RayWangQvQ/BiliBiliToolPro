using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazingQuartz.Jobs.Abstractions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazingQuartzJobs(this IServiceCollection services)
        {
            services.TryAddTransient<IDataMapValueResolver, DataMapValueResolver>();

            return services;
        }
    }
}
