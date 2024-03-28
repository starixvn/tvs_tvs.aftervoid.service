using System;

namespace TicketVoidingSystem.Models.SendEmailRecover;

public class TicketRecoverEmailModel
{
    public long TransId { get; set; }

    public int MatchId { get; set; }

    public string UserName { get; set; }

    public decimal Odds { get; set; }

    public byte OddsType { get; set; }

    public DateTime TransDate { get; set; }

    public int BetType { get; set; }

    public string BetTeam { get; set; }

    public decimal Hdp1 { get; set; }

    public decimal Hdp2 { get; set; }

    public short LiveHomeScore { get; set; }

    public short LiveAwayScore { get; set; }

    public decimal Stake { get; set; }

    public string GroupName { get; set; }

    public string Status { get; set; }

    public long HomeId { get; set; }

    public long AwayId { get; set; }

    public bool IsLiveIndicator { get; set; }

    public string WinLost { get; set; }

    public long BetId { get; set; }

    public long RefNo { get; set; }

    public int SportType { get; set; }

    public int LeagueId { get; set; }

    public string BetCheck { get; set; }

    public string TransDesc { get; set; }

    public bool IsCricket { get; set; }

    public string ExtraInfo { get; set; }

    public long BetdaqId { get; set; }

    public byte WalletType { get; set; }

    public long MainTransId { get; set; }

    public long? RefCode { get; set; }

    public decimal OddsSpread { get; set; }

    public byte SettlementStatus { get; set; }

    public decimal ActualStake { get; set; }

    public int CustId { get; set; }

    public int SiteDbGroupId { get; set; }

    public string BetFrom { get; set; }

    public bool IsCsd { get; set; }

    public bool IsItQa { get; set; }

    public bool IsItSupport { get; set; }

    public bool IsCashOut { get; set; }

    public bool IsSettled { get; set; }

    public string CurrencyName { get; set; }

    public decimal MasterOdds { get; set; }
}