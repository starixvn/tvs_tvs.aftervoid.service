using System;

namespace TicketVoidingSystem.Models;

public class TicketEmailModel : EmailSupport, SendMessageDbModel
{
    public decimal ActualStake { get; set; }
    public long AwayId { get; set; }
    public string BetCheck { get; set; }
    public long BetdaqId { get; set; }
    public long? BetId { get; set; }
    public string BetTeam { get; set; }
    public int BetTypeId { get; set; }
    public string ExtraInfo { get; set; }
    public decimal Handicap1 { get; set; }
    public decimal Handicap2 { get; set; }
    public long HomeId { get; set; }
    public int? MatchId { get; set; }
    public string MatchCode { get; set; }
    public bool IsLive { get; set; }
    public bool IsNeutral { get; set; }
    public int LeagueId { get; set; }
    public int LiveAwayScore { get; set; }
    public int LiveHomeScore { get; set; }
    public decimal MasterOdds { get; set; }
    public decimal Odds { get; set; }
    public decimal OddsSpread { get; set; }
    public int OddsType { get; set; }
    public long RefCode { get; set; }
    public string RefNo { get; set; }
    public string ShowTime { get; set; }
    public int SportTypeId { get; set; }
    public int SportType { get; set; }
    public decimal Stake { get; set; }
    public string Status { get; set; }
    public DateTime TransDate { get; set; }

    public string TransDesc { get; set; }
    public long TransId { get; set; }
    public string UserName { get; set; }
    public string OperatorID { get; set; }
    public byte WalletType { get; set; }
    public DateTime WinLostDate { get; set; }
    public bool IsMixParlay { get; set; }
    public string SubStatus { get; set; }
    public long SubTransId { get; set; }
    public string SubBetCheck { get; set; }
    public decimal SubOdds { get; set; }
    public string SubBetTeam { get; set; }
    public int SubBetType { get; set; }
    public decimal SubHdp1 { get; set; }
    public decimal SubHdp2 { get; set; }
    public string SubTransDesc { get; set; }
    public string CurrencyName { get; set; }
    public string UserName2 { get; set; }
    public string BetFrom { get; set; }
    public string EmailGroup { get; set; }
    public string Site { get; set; }
    public bool IsCashOut { get; set; }
    public bool IsCancelCashOut { get; set; }
    public bool IsCredit { get; set; }
    public bool IsItSupport { get; set; }
    public bool IsITSupportCreatedRequest { get; set; }
    public int Currency { get; set; }
    public int CustId { get; set; }
    public int Recommend { get; set; }
    public string AgUserName { get; set; }
    public int Mrecommend { get; set; }
    public string MagUserName { get; set; }
    public int Srecommend { get; set; }
    public string SagUserName { get; set; }
    public byte SettlementStatus { get; set; }
    public bool IsInstantSettlementTicket { get; set; }
    public long? MainTransId { get; set; }
    public bool IsVipMember { get; set; }
    public int LeagueGroupId { get; set; }
    public bool IsChinaView { get; set; }
    public int SpecialPlatform { get; set; }
    public string WinLost { get; set; }
    public int VoidingHistoryId { get; set; }
    public int SiteDbGroupId { get; set; }
    public string ZzzWinlost { get; set; }
}