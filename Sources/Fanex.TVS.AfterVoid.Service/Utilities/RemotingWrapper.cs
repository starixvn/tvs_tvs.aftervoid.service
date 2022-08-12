using System;
using System.Diagnostics.CodeAnalysis;
using Fanex.RemotingRef;
using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Utilities
{
    [ExcludeFromCodeCoverage]
    public static class RemotingWrapper
    {
        public static string GetBettype(object betTypeId, string language)
        {
            return new RemotingRefService().GetBetTypeName(betTypeId.ToString(), language);
        }

        public static string GetBettype(object betTypeId, object refNo)
        {
            return new RemotingRefService().GetBetTypeName(betTypeId.ToString(), LanguageCode.English, string.Empty, string.Empty, Convert.ToInt64(refNo));
        }

        public static string GetParentIdByBetTypeId(object betTypeId)
        {
            return new RemotingRefService().GetParentIdByBetTypeId(betTypeId.ToString(), betTypeId.ToString());
        }

        public static string GetLeagueName(object leagueId, string language)
        {
            return new RemotingRefService().GetLeagueName(ToString(leagueId), language, string.Empty);
        }

        public static string GetBFName(int id)
        {
            return new RemotingRefService().GetTeamName3rd(id.ToString(), LanguageCode.English);
        }

        public static string GetOddsTypeName(object oddsType)
        {
            return new RemotingRefService().GetOddsTypeName(ToString(oddsType), LanguageCode.English);
        }

        public static string GetTeamName(object teamId, string language)
        {
            return new RemotingRefService().GetTeamName(ToString(teamId), language, string.Empty);
        }

        public static string[] GetComboParlayName(string betTeam, string betCheck, decimal handicap1, decimal handicap2)
        {
            return new RemotingRefService().GetCombinationTypeCombo(betTeam, betCheck, Convert.ToInt32(handicap1), Convert.ToInt32(handicap2), LanguageCode.English);
        }

        public static string GetMatchCodeBetradar(string matchCode)
        {
            return new RemotingRefService().GetBetRadarMatchCodeFormat(matchCode, LanguageCode.English);
        }

        public static string GetSportName(string sportTypeId, string languageCode)
        {
            return new RemotingRefService().GetSportName(sportTypeId, languageCode);
        }

        private static string ToString(object item)
        {
            if (!ReferenceEquals(item, null) && item != DBNull.Value)
            {
                return item.ToString();
            }

            return string.Empty;
        }
    }
}