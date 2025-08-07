using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Fanex.TVS.AfterVoid.Service.Tests;

public class AppSettingFixture
{
    public AppSettingFixture()
    {
        var settings = new Dictionary<string, string> {
            {"LetstalkConfig:TargetName:TicketVoidingSystem", "Ticket-Voiding-System"},
            {"LetstalkConfig:TargetName:BCAutoVoid", "oneback-os-bc-auto-void"},
            {"LetstalkConfig:IsProdEnv", "false"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        AppSettingService.InitSetting(config);
        Console.WriteLine(JsonSerializer.Serialize(AppSettingService.AppSetting.LetstalkConfig));
        SiteDbGroupConstant.SetServerList(SiteDbGroupConstant.AllSiteDb);
    }
}