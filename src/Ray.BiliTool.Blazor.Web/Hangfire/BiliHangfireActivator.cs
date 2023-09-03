using Hangfire;
using System;
using Microsoft.Extensions.DependencyInjection;
using Ray.BiliTool.Blazor.Web.Models;

namespace Ray.BiliTool.Blazor.Web.Hangfire
{
    public class BiliHangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public BiliHangfireActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type)
        {
            using var scope = _serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService(type);
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            var serviceScope = _serviceProvider.CreateScope();

            return base.BeginScope(context);
        }
    }
}
