using System;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface AfterVoidService
    {
        void Execute();
    }

    public class AfterVoidServiceImp : AfterVoidService
    {
        private readonly LoggingService loggingService;

        public AfterVoidServiceImp(LoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        public void Execute()
        {
            loggingService.Info($"AfterVoidService Execute!!! {DateTime.Now}");
        }
    }
}