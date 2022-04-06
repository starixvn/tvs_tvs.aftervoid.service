using System;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class BetTrans
    {
        private readonly string betTypeName = string.Empty;

        public long TransId { get; set; }

        public int CustId { get; set; }

        public string Username { get; set; }

        public string Username2 { get; set; }

        public decimal Odds { get; set; }

        public decimal MOdds { get; set; }

        public int OddsType { get; set; }

        public DateTime TransDate { get; set; }

        public DateTime DateTime { get; set; }

        public int BetType { get; set; }

        public string BetTypeName => string.IsNullOrWhiteSpace(betTypeName) ?
            RemotingWrapper.GetBettype(BetType, RefNo) : betTypeName;

        public string BetTeam { get; set; }

        public string BetFrom { get; set; }

        public decimal Hdp1 { get; set; }

        public decimal Hdp2 { get; set; }

        public short LiveHomeScore { get; set; }

        public short LiveAwayScore { get; set; }

        public string Creator { get; set; }

        public decimal Stake { get; set; }

        public decimal ActualStake { get; set; }

        public string CurrencyName { get; set; }

        public bool IsDeposit { get; set; }

        public string Site { get; set; }

        public string Status { get; set; }

        public int Recommend { get; set; }

        public bool SecAcceptFlag { get; set; }

        public long HomeId { get; set; }

        public long AwayId { get; set; }

        public bool IsLiveIndicator { get; set; }

        public bool IsNeutral { get; set; }

        public string WinLost { get; set; }

        public bool IsMixParlay { get; set; }

        public string Selection { get; set; }

        public long BetId { get; set; }

        public long RefNo { get; set; }

        public decimal StakePositiontaking { get; set; }

        public int SportType { get; set; }

        public int LeagueId { get; set; }

        public bool IsNegative { get; set; }

        public decimal Comm { get; set; }

        public string MatchCode { get; set; }

        public long FollowedId { get; set; }

        public string FollowedUsername { get; set; }

        public string BetCheck { get; set; }

        public string TransDesc { get; set; }

        public string SoldId { get; set; }

        public string SiteId { get; set; }

        public short MainStatusId { get; set; }

        public bool IsCricket { get; set; }

        public bool IsHideComm { get; set; }

        public string AgUserName { get; set; }

        public int Mrecommend { get; set; }

        public string MagUserName { get; set; }

        public int Srecommend { get; set; }

        public string SagUserName { get; set; }

        public bool IsReal { get; set; }

        public bool IsItQa { get; set; }

        public bool IsItSupport { get; set; }

        public bool IsCashOut { get; set; }

        public bool IsMetricGaming { get; set; }

        public bool IsSettled { get; set; }

        public int Currency { get; set; }

        public string GameEvent { get; set; }

        public string EmailGroup { get; set; }

        public short OriginalBetType { get; set; }

        public string ExtraInfo { get; set; }

        public long BetdaqId { get; set; }

        public byte WalletType { get; set; }

        public long? RefCode { get; set; }

        public decimal OddsSpread { get; set; }
    }
}