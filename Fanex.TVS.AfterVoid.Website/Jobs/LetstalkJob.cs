using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using ONEbook.UM.Services;
using Quartz;

namespace Fanex.TVS.AfterVoid.Website.Jobs
{
    [ExcludeFromCodeCoverage]
    public class LetstalkJob : IJob
    {
        private readonly SendLetstalkService sendLetstalkService;
        private readonly LoggingService loggingService;
        private readonly IMaintenanceService maintenanceService;

        public LetstalkJob(IMaintenanceService maintenanceService, LoggingService loggingService,
            SendLetstalkService sendLetstalkService)
        {
            this.maintenanceService = maintenanceService;
            this.loggingService = loggingService;
            this.sendLetstalkService = sendLetstalkService;
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
                        await sendLetstalkService.Execute();
                    }

                    AppConfigService.AppConfig.OnProcessing = false;
                }
            }
            catch (Exception ex)
            {
                AppConfigService.AppConfig.OnProcessing = false;
                loggingService.Error("LetstalkJob execute failed!", ex);
            }
        }
    }
}