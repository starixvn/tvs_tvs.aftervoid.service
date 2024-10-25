using System;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Constants;
using TicketVoidingSystem.Constants;

namespace TicketVoidingSystem.Services;

public interface RemotingRefWrapperService
{
    string GetBetType(object betTypeId, string language);

    string GetBetType(object betTypeId, object refNo);

    string GetDisplayedBetTypeName(object betTypeId, object refNo);

    string GetParentIdByBetTypeId(object betTypeId);

    string GetLeagueName(int sportTypeId, object leagueId, string language);

    string GetTeamName(int sportTypeId, object teamId, string language);

    string GetBFName(int id);

    string GetOddsTypeName(object oddsType);

    string[] GetComboParlayName(string betTeam, string betCheck, decimal handicap1, decimal handicap2);

    string GetSportName(string sportTypeId, string languageCode);
}

public class RemotingRefWrapperServiceImp(IRemotingRefService remotingRefService) : RemotingRefWrapperService
{
    public string GetBetType(object betTypeId, string language)
    {
        return remotingRefService.GetBetTypeName(betTypeId.ToString(), language);
    }

    public string GetBetType(object betTypeId, object refNo)
    {
        return remotingRefService.GetBetTypeName(betTypeId.ToString(), LanguageCode.English, string.Empty, string.Empty, Convert.ToInt64(refNo));
    }

    public string GetDisplayedBetTypeName(object betTypeId, object refNo)
    {
        return remotingRefService.GetDisplayedBetTypeName(betTypeId.ToString(), string.Empty, Convert.ToInt64(refNo), LanguageCode.English, string.Empty);
    }

    public string GetParentIdByBetTypeId(object betTypeId)
    {
        return remotingRefService.GetParentIdByBetTypeId(betTypeId.ToString(), betTypeId.ToString());
    }

    public string GetLeagueName(int sportTypeId, object leagueId, string language)
    {
        return IsSportTypeOfVirtualSports5(sportTypeId)
            ? remotingRefService.GetLeagueName3rd(ToString(leagueId), language, string.Empty)
            : remotingRefService.GetLeagueName(ToString(leagueId), language, string.Empty);
    }

    public string GetTeamName(int sportTypeId, object teamId, string language)
    {
        return IsSportTypeOfVirtualSports5(sportTypeId)
            ? remotingRefService.GetTeamName3rd(ToString(teamId), language, string.Empty)
            : remotingRefService.GetTeamName(ToString(teamId), language, string.Empty);
    }

    public string GetBFName(int id)
    {
        return remotingRefService.GetTeamName3rd(id.ToString(), LanguageCode.English);
    }

    public string GetOddsTypeName(object oddsType)
    {
        return remotingRefService.GetOddsTypeName(ToString(oddsType), LanguageCode.English);
    }

    public string[] GetComboParlayName(string betTeam, string betCheck, decimal handicap1, decimal handicap2)
    {
        return remotingRefService.GetCombinationTypeCombo(betTeam, betCheck, Convert.ToInt32(handicap1), Convert.ToInt32(handicap2), LanguageCode.English);
    }

    public string GetSportName(string sportTypeId, string languageCode)
    {
        return remotingRefService.GetSportName(sportTypeId, languageCode);
    }

    private static string ToString(object item)
    {
        return item is not null && item != DBNull.Value ? item.ToString() : string.Empty;
    }

    private static bool IsSportTypeOfVirtualSports5(int sportType)
    {
        return sportType is VirtualSportSportTypeConstant.VirtuallySoccer;
    }
}