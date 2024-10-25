using System.Collections.Generic;
using System.Linq;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace TicketVoidingSystem.Constants;

public static class SiteDbGroupConstant
{
    public const int CentralDb = 0;
    public const int SiteDb1 = 1;
    public const int SiteDb2 = 2;
    public const int SiteDb3 = 3;

    static SiteDbGroupConstant()
    {
        var allSiteDb = new List<SiteDbGroupModel>
        {
            new()
            {
                Id = CentralDb,
                ServerName = "DBCTG"
            },
            new()
            {
                Id = SiteDb1,
                ServerName = "DBSITE01"
            },
            new()
            {
                Id = SiteDb2,
                ServerName = "DBSITE02"
            },
            new()
            {
                Id = SiteDb3,
                ServerName = "DBSITE03"
            }
        };

        var ignoreSiteDbList = !string.IsNullOrWhiteSpace(AppSettingService.AppSetting.IgnoreSiteDb)
            ? AppSettingService.AppSetting.IgnoreSiteDb.Split(',').Select(int.Parse)
            : new List<int>();

        ServerList = ignoreSiteDbList.Any() ? allSiteDb.Where(site => ignoreSiteDbList.All(y => y != site.Id)).ToList() : allSiteDb;
    }

    public static List<SiteDbGroupModel> ServerList { get; }
}