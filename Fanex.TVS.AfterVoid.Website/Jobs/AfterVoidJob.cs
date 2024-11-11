using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using ONEbook.UM.Services;
using Quartz;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

[ExcludeFromCodeCoverage(Justification = "non-logic")]
public class AfterVoidJob(
    IMaintenanceService maintenanceService,
    LoggingService loggingService,
    AfterVoidService afterVoidService)
    : IJob
{
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
            await loggingService.ErrorAsync("AfterVoidJob execute failed!", ex);
        }
    }
}