using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using ONEbook.UM.Services;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

using System.Threading;
using Microsoft.Extensions.Hosting;

public class LetstalkJob(
    IMaintenanceService maintenanceService,
    LoggingService loggingService,
    SendLetstalkService sendLetstalkService)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await TryDoTask();
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task TryDoTask()
    {
        try
        {
            await DoTask();
        }
        catch (Exception ex)
        {
            await loggingService.ErrorAsync("LetstalkJob execute failed!", ex);
        }
        finally
        {
            AppSettingService.AppSetting.OnProcessingLetstalk = false;
        }
    }

    private async Task DoTask()
    {
        if (AppSettingService.AppSetting.OnProcessingLetstalk)
        {
            return;
        }

        if (await maintenanceService.IsUnderMaintenanceAsync())
        {
            return;
        }

        AppSettingService.AppSetting.OnProcessingLetstalk = true;

        await sendLetstalkService.Execute();

        AppSettingService.AppSetting.OnProcessingLetstalk = false;
    }
}