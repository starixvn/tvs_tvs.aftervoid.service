using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Service.Constants;

public static class SportTypeConstant
{
    public const int VirtualFootballCup = 178;     // Virtual Sports 3

    public const int VirtualFootballLeague = 179;     // Virtual Sports 3

    public const int VirtualCockFighting = 188;     // Virtual Sports 4

    public const int VirtualSportsVs4 = 9908;     // Virtual Sports 4

    public const int VirtuallySoccer = 147;     // Virtual Sports 5

    public static Dictionary<int, string> RefIdVirtualSports = new()
    {
        { VirtualFootballCup, "LVS_MatchCode" },
        { VirtualFootballLeague, "LVS_MatchCode" },
        { VirtualCockFighting, "EGVS_MatchCode" },
        { VirtualSportsVs4, "EGVS_MatchCode" },
        { VirtuallySoccer, "VYVS_MatchCode" }
    };
}