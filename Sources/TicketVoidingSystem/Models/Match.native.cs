using System;
using Newtonsoft.Json;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Resources;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Models
{
    public class Match
    {
        public int MatchId { get; set; }

        public int HomeId { get; set; }

        public string HomeName => IsNeutral ?
            RemotingWrapper.GetTeamName(HomeId, LanguageCode.English) + " (N)" : RemotingWrapper.GetTeamName(HomeId, LanguageCode.English);

        public string HomeNameSimplified => IsNeutral ?
            RemotingWrapper.GetTeamName(HomeId, LanguageCode.ChineseSimplified) + " (中)" : RemotingWrapper.GetTeamName(HomeId, LanguageCode.ChineseSimplified);

        public string HomeNameNewSimplified => IsNeutral ?
            RemotingWrapper.GetTeamName(HomeId, LanguageCode.NewChineseSimplified) + " (中)" : RemotingWrapper.GetTeamName(HomeId, LanguageCode.NewChineseSimplified);

        public string HomeNameTraditional => IsNeutral ?
            RemotingWrapper.GetTeamName(HomeId, LanguageCode.ChineseTraditional) + " (中)" : RemotingWrapper.GetTeamName(HomeId, LanguageCode.ChineseTraditional);

        public int AwayId { get; set; }

        public string AwayName => RemotingWrapper.GetTeamName(AwayId, LanguageCode.English);

        public string AwayNameSimplified => RemotingWrapper.GetTeamName(AwayId, LanguageCode.ChineseSimplified);

        public string AwayNameNewSimplified => RemotingWrapper.GetTeamName(AwayId, LanguageCode.NewChineseSimplified);

        public string AwayNameTraditional => RemotingWrapper.GetTeamName(AwayId, LanguageCode.ChineseTraditional);

        public string MatchCode { get; set; }

        public DateTime EventDate { get; set; }

        public DateTime DateTime { get; set; }

        public int LeagueId { get; set; }

        public int LeagueGroupId { get; set; }

        public string LeagueName => RemotingWrapper.GetLeagueName(LeagueId, LanguageCode.English);

        public string LeagueNameSimplified => RemotingWrapper.GetLeagueName(LeagueId, LanguageCode.ChineseSimplified);

        public string LeagueNameNewSimplified => RemotingWrapper.GetLeagueName(LeagueId, LanguageCode.NewChineseSimplified);

        public string LeagueNameTraditional => RemotingWrapper.GetLeagueName(LeagueId, LanguageCode.ChineseTraditional);

        public bool IsOutright { get; set; }

        public string MatchEvent
        {
            get
            {
                switch (SportType)
                {
                    case 161:
                        return Labels.NumberGameNo + MatchCode;

                    case 164:
                        return $"Happy 5 No .{MatchCode}";

                    case 160:
                        return $"Lucky 3 Game No.{MatchCode}";

                    case 207:
                        return $"{RemotingWrapper.GetBFName(HomeId)} - vs - {RemotingWrapper.GetBFName(AwayId)}";

                    default:
                        return IsOutright ? $"{HomeName} - {LeagueName}" : $"{HomeName} - vs - {AwayName}";
                }
            }
        }

        public string MatchEventSimplified
        {
            get
            {
                if (SportType == 161 || SportType == 160 || SportType == 207)
                {
                    return string.Empty;
                }

                return IsOutright ? $"{HomeNameSimplified} - {LeagueNameSimplified}" : $"{HomeNameSimplified} - vs - {AwayNameSimplified}";
            }
        }

        public string MatchEventNewSimplified
        {
            get
            {
                if (SportType == 161 || SportType == 160 || SportType == 207)
                {
                    return string.Empty;
                }

                return IsOutright ? $"{HomeNameNewSimplified} - {LeagueNameNewSimplified}" : $"{HomeNameNewSimplified} - vs - {AwayNameNewSimplified}";
            }
        }

        public bool IsNeutral { get; set; }

        public string ShowTime { get; set; }

        [JsonConverter(typeof(ClientDateGMT8JsonConverter))]
        public DateTime KickOffTime { get; set; }

        public int SportType { get; set; }

        public string SportTypeName => RemotingWrapper.GetSportName(SportType.ToString(), LanguageCode.English);

        public string SportTypeNameSimplified => RemotingWrapper.GetSportName(SportType.ToString(), LanguageCode.ChineseSimplified);

        public string SportTypeNameNewSimplified => RemotingWrapper.GetSportName(SportType.ToString(), LanguageCode.NewChineseSimplified);

        public string SportTypeNameTraditional => RemotingWrapper.GetSportName(SportType.ToString(), LanguageCode.ChineseTraditional);
    }
}