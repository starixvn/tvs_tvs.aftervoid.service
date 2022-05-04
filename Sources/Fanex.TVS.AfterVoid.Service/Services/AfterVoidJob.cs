using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Services;
using Fanex.TVS.AfterVoid.Core.Services.Main;
using ONEbook.UM.Services;
using Quartz;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    [ExcludeFromCodeCoverage]
    public class AfterVoidJob : IJob
    {
        private readonly AfterVoidService afterVoidService;
        private readonly LoggingService loggingService;
        private readonly IMaintenanceService maintenanceService;

        public AfterVoidJob(IMaintenanceService maintenanceService, LoggingService loggingService,
            AfterVoidService afterVoidService)
        {
            this.maintenanceService = maintenanceService;
            this.loggingService = loggingService;
            this.afterVoidService = afterVoidService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (!AppConfigService.AppConfig.OnProcessing)
                {
                    AppConfigService.AppConfig.OnProcessing = true;

                    if (!await maintenanceService.IsUnderMaintenanceAsync())
                    {
                        await afterVoidService.Execute();
                    }

                    AppConfigService.AppConfig.OnProcessing = false;
                }
            }
            catch (Exception ex)
            {
                AppConfigService.AppConfig.OnProcessing = false;
                loggingService.Error("AfterVoidJob execute failed!", ex);
            }
        }
    }
}