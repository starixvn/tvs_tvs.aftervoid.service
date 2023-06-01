using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using ONEbook.UM.Services;
using Quartz;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

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
            if (!AppSettingService.AppSetting.OnProcessingLetstalk)
            {
                AppSettingService.AppSetting.OnProcessingLetstalk = true;

                if (!await maintenanceService.IsUnderMaintenanceAsync())
                {
                    await sendLetstalkService.Execute();
                }

                AppSettingService.AppSetting.OnProcessingLetstalk = false;
            }
        }
        catch (Exception ex)
        {
            AppSettingService.AppSetting.OnProcessingLetstalk = false;
            await loggingService.ErrorAsync("LetstalkJob execute failed!", ex);
        }
    }
}