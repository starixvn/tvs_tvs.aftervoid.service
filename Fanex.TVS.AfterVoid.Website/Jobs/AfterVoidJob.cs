using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using ONEbook.UM.Services;
using Quartz;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

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
            if (!AppSettingService.AppSetting.OnProcessingAfterVoid)
            {
                AppSettingService.AppSetting.OnProcessingAfterVoid = true;

                if (!await maintenanceService.IsUnderMaintenanceAsync())
                {
                    await afterVoidService.Execute();
                }

                AppSettingService.AppSetting.OnProcessingAfterVoid = false;
            }
        }
        catch (Exception ex)
        {
            AppSettingService.AppSetting.OnProcessingAfterVoid = false;
            loggingService.Error("AfterVoidJob execute failed!", ex);
        }
    }
}