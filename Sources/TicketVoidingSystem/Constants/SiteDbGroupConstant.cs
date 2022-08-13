using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace TicketVoidingSystem.Constants
{
    public static class SiteDbGroupConstant
    {
        public const int CentralDb = 0;
        public const int SiteDb1 = 1;

        public static List<SiteDbGroupModel> ServerList =>
            new()
            {
                new SiteDbGroupModel
                {
                    Id = CentralDb,
                    ServerName = nameof(CentralDb)
                },
                new SiteDbGroupModel
                {
                    Id = SiteDb1,
                    ServerName = nameof(SiteDb1)
                }
            };
    }
}