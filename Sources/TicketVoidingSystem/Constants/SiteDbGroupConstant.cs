using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace TicketVoidingSystem.Constants;

public static class SiteDbGroupConstant
{
    public const int CentralDb = 0;
    public const int SiteDb1 = 1;
    public const int SiteDb2 = 2;
    public const int SiteDb3 = 3;

    public static readonly List<SiteDbGroupModel> AllSiteDb =
    [
        new() { Id = CentralDb, ServerName = "DBCTG" },
        new() { Id = SiteDb1, ServerName = "DBSITE01" },
        new() { Id = SiteDb2, ServerName = "DBSITE02" },
        new() { Id = SiteDb3, ServerName = "DBSITE03" }
    ];

    private static List<SiteDbGroupModel> _serverList = [];

    public static IReadOnlyList<SiteDbGroupModel> ServerList => _serverList;

    public static void SetServerList(List<SiteDbGroupModel> servers)
    {
        _serverList = servers;
    }
}