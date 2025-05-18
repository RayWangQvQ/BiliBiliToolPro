using System;
using System.Reflection;
using BlazingQuartz.Core.History;
using BlazingQuartz.Core.Services;
using BlazingQuartz.Jobs;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Ray.BiliBiliTool.Infrastructure.EF;

namespace BlazingQuartz.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazingQuartz(this IServiceCollection services)
        {
            services.AddBlazingQuartzJobs();

            services.TryAddSingleton<ISchedulerDefinitionService, SchedulerDefinitionService>();
            services.AddTransient<ISchedulerService, SchedulerService>();

            var schListenerSvc = new SchedulerListenerService();
            services.TryAddSingleton<ISchedulerListenerService>(schListenerSvc);
            services.AddSingleton<ITriggerListener>(schListenerSvc);
            services.AddSingleton<IJobListener>(schListenerSvc);
            services.AddSingleton<ISchedulerListener>(schListenerSvc);

            services.AddScoped<IExecutionLogStore, ExecutionLogStore>();
            services.AddScoped<IExecutionLogService, ExecutionLogService>();

            services.AddSingleton<IExecutionLogRawSqlProvider, BaseExecutionLogRawSqlProvider>();

            services.AddHostedService<SchedulerEventLoggingService>();

            return services;
        }
    }
}
