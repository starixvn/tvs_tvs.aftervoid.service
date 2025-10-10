using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public enum LetstalkGroupType
{
    VoidSettledBets,
    VoidAbnormalBets,
    BcAutoVoid
}

public static class GroupFactory
{
    public static string GetGroup(LetstalkGroupType type, bool isAllUusTicket)
    {
        var config = AppSettingService.AppSetting.LetstalkConfig;
        return config.IsProdEnv switch
        {
            true => GetConfigProdEnv(type, isAllUusTicket),
            false => GetConfigNonProdEnv(isAllUusTicket),
        };
    }

    private static string GetConfigProdEnv(LetstalkGroupType type, bool isAllUusTicket)
    {
        var config = AppSettingService.AppSetting.LetstalkConfig;

        if (isAllUusTicket)
        {
            return config.ItGroup;
        }

        return type switch
        {
            LetstalkGroupType.VoidSettledBets => config.VoidSettledBets,
            LetstalkGroupType.VoidAbnormalBets => config.VoidAbnormalBets,
            LetstalkGroupType.BcAutoVoid => config.TargetName.BCAutoVoid,
            _ => config.ItGroup
        };
    }

    private static string GetConfigNonProdEnv(bool isAllUusTicket)
    {
        var config = AppSettingService.AppSetting.LetstalkConfig;

        return isAllUusTicket ? config.ItGroup : config.ItGroupMockProd;
    }
}