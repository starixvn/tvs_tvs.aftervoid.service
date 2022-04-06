namespace Fanex.TVS.AfterVoid.Core.SendEmail.Extensions
{
    using System.Collections.Generic;

    public static class LeagueExtension
    {
        private static readonly List<int> LeagueGroupIdsSaBa = new List<int> { 42, 74, 108 };

        public static bool IsLeagueGroupIdSaBa(this int leagueGroupId)
        {
            return LeagueGroupIdsSaBa.Contains(leagueGroupId);
        }
    }
}