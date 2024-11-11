using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fanex.TVS.AfterVoid.Service.Constants;

public static class VirtualSportSportTypeConstant
{
    public const int VirtualFootballCup = 178;     // Virtual Sports 3

    public const int VirtualFootballLeague = 179;     // Virtual Sports 3

    public const int VirtualCockFighting = 188;     // Virtual Sports 4

    public const int VirtualSportsVs4 = 9908;     // Virtual Sports 4

    public const int VirtuallySoccer = 147;     // Virtual Sports 5

    public static readonly ImmutableDictionary<int, string> RefIdVirtualSports = new Dictionary<int, string>
    {
        { VirtualFootballCup, "LVS_MatchCode" },
        { VirtualFootballLeague, "LVS_MatchCode" },
        { VirtualCockFighting, "EGVS_MatchCode" },
        { VirtualSportsVs4, "EGVS_MatchCode" },
        { VirtuallySoccer, "VYVS_MatchCode" }
    }.ToImmutableDictionary();
}