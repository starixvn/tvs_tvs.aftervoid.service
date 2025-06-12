using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Tests;

public class AppSettingFixture
{
    public AppSettingFixture()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        AppSettingService.InitSetting(config);
        SiteDbGroupConstant.SetServerList(SiteDbGroupConstant.AllSiteDb);
    }
}