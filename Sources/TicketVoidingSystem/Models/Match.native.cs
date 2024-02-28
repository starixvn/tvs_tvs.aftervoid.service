using System;
using Newtonsoft.Json;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Models;

public class Match
{
    public int MatchId { get; set; }

    public int HomeId { get; set; }

    public string HomeName { get; set; }

    public string HomeNameSimplified { get; set; }

    public string HomeNameNewSimplified { get; set; }

    public string HomeNameTraditional { get; set; }

    public int AwayId { get; set; }

    public string AwayName { get; set; }

    public string AwayNameSimplified { get; set; }

    public string AwayNameNewSimplified { get; set; }

    public string AwayNameTraditional { get; set; }

    public string MatchCode { get; set; }

    public DateTime EventDate { get; set; }

    public DateTime DateTime { get; set; }

    public int LeagueId { get; set; }

    public int LeagueGroupId { get; set; }

    public string LeagueName { get; set; }

    public string LeagueNameSimplified { get; set; }

    public string LeagueNameNewSimplified { get; set; }

    public string LeagueNameTraditional { get; set; }

    public bool IsOutright { get; set; }

    public string MatchEvent { get; set; }

    public string MatchEventSimplified { get; set; }

    public string MatchEventNewSimplified { get; set; }

    public bool IsNeutral { get; set; }

    public string ShowTime { get; set; }

    [JsonConverter(typeof(ClientDateGMT8JsonConverter))]
    public DateTime KickOffTime { get; set; }

    public int SportType { get; set; }

    public string SportTypeName { get; set; }

    public string SportTypeNameSimplified { get; set; }

    public string SportTypeNameNewSimplified { get; set; }

    public string SportTypeNameTraditional { get; set; }

    public string OriginLeagueName { get; set; }

    public string OriginLeagueNameSimplified { get; set; }

    public string OriginLeagueNameNewSimplified { get; set; }

    public string OriginLeagueNameTraditional { get; set; }
}