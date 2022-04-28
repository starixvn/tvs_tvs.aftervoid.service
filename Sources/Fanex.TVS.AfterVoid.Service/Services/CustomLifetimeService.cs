using System;
using System.Threading;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Services;
using Microsoft.Extensions.Hosting;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    public class CustomLifetimeService : IHostedService
    {
        private readonly LoggingService loggingService;

        public CustomLifetimeService(
            LoggingService loggingService,
            IHostApplicationLifetime appLifetime)
        {
            this.loggingService = loggingService;

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //// loggingService.Info("1. StartAsync has been called.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //// loggingService.Info("4. StopAsync has been called.");

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            loggingService.Info($"Fanex.TVS.AfterVoid.Service - Started - {DateTime.Now}");
        }

        private void OnStopping()
        {
            //// loggingService.Info("3. OnStopping has been called.");
        }

        private void OnStopped()
        {
            loggingService.Info($"Fanex.TVS.AfterVoid.Service - STOPPED!!! - {DateTime.Now}");
        }
    }
}