using System.Collections.Generic;

namespace TicketVoidingSystem.Extensions;

public static class LeagueExtension
{
    private static readonly List<int> LeagueGroupIdsSaBa = [42, 74, 108, 113, 122, 151, 252];

    public static bool IsLeagueGroupIdSaBa(this int leagueGroupId)
    {
        return LeagueGroupIdsSaBa.Contains(leagueGroupId);
    }
}