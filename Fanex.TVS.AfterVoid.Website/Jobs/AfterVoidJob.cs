using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using ONEbook.UM.Services;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

using System.Threading;
using Microsoft.Extensions.Hosting;

public class AfterVoidJob(
    IMaintenanceService maintenanceService,
    LoggingService loggingService,
    AfterVoidService afterVoidService)
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
            await loggingService.ErrorAsync("AfterVoidJob execute failed!", ex);
        }
        finally
        {
            AppSettingService.AppSetting.OnProcessingAfterVoid = false;
        }
    }

    private async Task DoTask()
    {
        if (AppSettingService.AppSetting.OnProcessingAfterVoid)
        {
            return;
        }

        if (await maintenanceService.IsUnderMaintenanceAsync())
        {
            return;
        }

        AppSettingService.AppSetting.OnProcessingAfterVoid = true;

        await afterVoidService.Execute();

        AppSettingService.AppSetting.OnProcessingAfterVoid = false;
    }
}