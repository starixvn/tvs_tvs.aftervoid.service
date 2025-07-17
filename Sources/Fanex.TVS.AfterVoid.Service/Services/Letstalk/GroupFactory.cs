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
    public static string GetGroup(LetstalkGroupType type, bool isHasUusTicket)
    {
        var config = AppSettingService.AppSetting.LetstalkConfig;

        if (!config.IsProdEnv || isHasUusTicket)
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
}