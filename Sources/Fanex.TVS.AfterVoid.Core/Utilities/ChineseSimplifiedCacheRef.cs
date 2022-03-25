using System.Collections.Generic;
using Fanex.BetList.Core.TicketHelper;
using Fanex.RemotingRef;

namespace Fanex.TVS.AfterVoid.Core.Utilities
{
    public class ChineseSimplifiedCacheRef : ICacheRef
    {
        public static readonly string DefaultValue = string.Empty;

        private readonly IRemotingRefService _remotingRefService;

        public ChineseSimplifiedCacheRef(IRemotingRefService remotingRefService = null)
        {
            _remotingRefService = remotingRefService ?? new RemotingRefService();
        }

        private const string CurrentLanguage = "zh-CN";

        public string GetBetType(string betTypeId, string resourceId, long refNo)
            => _remotingRefService.GetBetTypeName(betTypeId, CurrentLanguage, DefaultValue, resourceId, refNo);

        public string GetParentBetTypeIdByBetTypeId(string betTypeId, string defaultParentBetTypeId)
            => _remotingRefService.GetParentIdByBetTypeId(betTypeId, defaultParentBetTypeId);

        public string GetSportName(string sportId)
            => _remotingRefService.GetSportName(sportId, CurrentLanguage);

        public string GetTeamName(string teamId)
            => _remotingRefService.GetTeamName(teamId, CurrentLanguage);

        public string GetTeamName3rd(string team3rdId)
            => _remotingRefService.GetTeamName3rd(team3rdId, CurrentLanguage);

        public string GetRouletteBetItem(string itemId)
            => _remotingRefService.GetRouletteBetItem(itemId, CurrentLanguage);

        public string GetCasinoBetItem(string betStatusId)
            => _remotingRefService.GetCasinoBetItem(betStatusId, CurrentLanguage, DefaultValue);

        public string GetLeagueName(string leagueId)
            => _remotingRefService.GetLeagueName(leagueId, CurrentLanguage);

        public string GetLeagueName3rd(string league3rdId)
            => _remotingRefService.GetLeagueName3rd(league3rdId, CurrentLanguage);

        public string GetHorseTeamName(string codeId)
            => _remotingRefService.GetHorseTeamName(codeId, CurrentLanguage, DefaultValue);

        public string GetOddsTypeName(string oddsTypeId)
            => _remotingRefService.GetOddsTypeName(oddsTypeId, CurrentLanguage);

#pragma warning disable S3874 // "out" and "ref" parameters should not be used

        public string GetKenoBetChoiceName(string kenoBetChoiceId, out string kenoBetTypeId)
            => _remotingRefService.GetKenoBetChoice(kenoBetChoiceId, CurrentLanguage, out kenoBetTypeId);

#pragma warning restore S3874 // "out" and "ref" parameters should not be used

        public string GetKenoBetTypeName(string kenoBetTypeId)
            => _remotingRefService.GetKenoBetType(kenoBetTypeId, CurrentLanguage);

        public string GetResourceData(string refId, string resourceId)
            => _remotingRefService.GetResourceDataByRefIdAndResourceId(refId, resourceId, CurrentLanguage, DefaultValue);

        public IList<string> GetCombinationTypeCombo(string betTeam, string betCheck, int handicap1, int handicap2)
            => _remotingRefService.GetCombinationTypeCombo(betTeam, betCheck, handicap1, handicap2, CurrentLanguage);

        public string GetFastMarketsChoice(string betTypeId, string betTeam, int handicap1, int handicap2)
            => _remotingRefService.GetFastMarketsChoice(betTypeId, betTeam, handicap1, handicap2, CurrentLanguage);

#pragma warning disable S3874 // "out" and "ref" parameters should not be used

        public string GetLotteryBetChoiceName(string lotteryBetChoiceId, out string lotteryBetTypeId)
            => _remotingRefService.GetLotteryBetChoice(lotteryBetChoiceId, CurrentLanguage, out lotteryBetTypeId);

#pragma warning restore S3874 // "out" and "ref" parameters should not be used

        public string GetLotteryBetTypeName(string lotteryBettypeId)
            => _remotingRefService.GetLotteryBetType(lotteryBettypeId, CurrentLanguage);

        public string GetBetRadarMatchCodeFormat(string matchCode)
            => _remotingRefService.GetBetRadarMatchCodeFormat(matchCode, CurrentLanguage);

        public string GetDisplayedBetType(string betTypeId, string resourceId, long refNo)
            => _remotingRefService.GetDisplayedBetTypeName(betTypeId, resourceId, refNo, CurrentLanguage, DefaultValue);

        public string GetTableGamesBetChoiceName(string tableGamesBetChoiceId, out string tableGamesBetTypeId)
            => _remotingRefService.GetTableGamesBetChoice(tableGamesBetChoiceId, CurrentLanguage, out tableGamesBetTypeId);

        public string GetTableGamesBetTypeName(string tableGamesBetTypeId)
            => _remotingRefService.GetTableGamesBetType(tableGamesBetTypeId, CurrentLanguage);
    }
}