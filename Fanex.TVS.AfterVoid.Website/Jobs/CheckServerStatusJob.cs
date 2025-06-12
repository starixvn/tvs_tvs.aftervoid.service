using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fanex.SiteDbConnectionManager.Client.Services;
using Microsoft.Extensions.Hosting;
using ONEbook.UM.Services;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Jobs;

public class CheckServerStatusJob(IMaintenanceService maintenanceService, LoggingService loggingService,
    SiteDbClientService siteDbClientService) : BackgroundService
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
            await loggingService.ErrorAsync($"{nameof(CheckServerStatusJob)} execute failed!", ex);
        }
    }

    private async Task DoTask()
    {
        if (await maintenanceService.IsUnderMaintenanceAsync())
        {
            return;
        }

        var onlineServer = await GetOnlineServer();
        var currentOnlineServerId = onlineServer.Select(x => x.Id).ToList();
        var previousOnlineServer = SiteDbGroupConstant.ServerList;
        var previousOnlineServerId = previousOnlineServer.Select(x => x.Id);
        var isSameSet = new HashSet<int>(currentOnlineServerId).SetEquals(previousOnlineServerId);

        if (!isSameSet)
        {
            SiteDbGroupConstant.SetServerList(onlineServer);
            await loggingService.InfoAsync($"change-sitedb-status" +
                                           $"{Environment.NewLine}Current: {JsonSerializer.Serialize(onlineServer)}" +
                                           $"{Environment.NewLine}Previous: {JsonSerializer.Serialize(previousOnlineServer)}");
        }
    }

    private async Task<List<SiteDbGroupModel>> GetOnlineServer()
    {
        var onlineServer = await siteDbClientService.GetActiveSiteDbAsync();
        return onlineServer.Select(x => new SiteDbGroupModel
        {
            Id = x.SiteDbGroupId,
            IsOnline = x.Status,
            ServerName = SiteDbGroupConstant.AllSiteDb.Find(y => y.Id == x.SiteDbGroupId)?.ServerName
        }).ToList();
    }
}