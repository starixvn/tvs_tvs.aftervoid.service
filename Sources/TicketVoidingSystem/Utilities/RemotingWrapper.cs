using System;
using System.Diagnostics.CodeAnalysis;
using Fanex.RemotingRef;
using TicketVoidingSystem.Constants;

namespace TicketVoidingSystem.Utilities;

[ExcludeFromCodeCoverage]
public static class RemotingWrapper
{
    public static string GetLeagueName(object leagueId, string language)
    {
        return new RemotingRefService().GetLeagueName(ToString(leagueId), language, string.Empty);
    }

    public static string GetBFName(int id)
    {
        return new RemotingRefService().GetTeamName3rd(id.ToString(), LanguageCode.English);
    }

    public static string GetTeamName(object teamId, string language)
    {
        return new RemotingRefService().GetTeamName(ToString(teamId), language, string.Empty);
    }

    public static string GetSportName(string sportTypeId, string languageCode)
    {
        return new RemotingRefService().GetSportName(sportTypeId, languageCode);
    }

    private static string ToString(object item)
    {
        if (item is not null && item != DBNull.Value)
        {
            return item.ToString();
        }

        return string.Empty;
    }
}