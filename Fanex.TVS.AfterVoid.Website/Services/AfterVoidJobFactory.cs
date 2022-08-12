using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    [ExcludeFromCodeCoverage]
    public class AfterVoidJobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        public AfterVoidJobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return serviceProvider.GetService<AfterVoidJob>();
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}