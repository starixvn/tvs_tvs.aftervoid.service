using System.Linq;
using TicketVoidingSystem.Constants;

namespace TicketVoidingSystem.Utilities
{
    public static class Helper
    {
        public static string GetSettingKeyBySiteDbGroupId(int siteDbGroupId, string spName)
        {
            var serverName = SiteDbGroupConstant.ServerList.First(x => x.Id == siteDbGroupId).ServerName;

            return $"{serverName}.{spName}";
        }
    }
}