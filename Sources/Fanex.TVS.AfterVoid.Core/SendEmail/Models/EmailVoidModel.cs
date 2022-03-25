using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Models
{
    public class EmailVoidModel
    {
        public string NegativeBalanceUsers { get; set; }
        public Match Match { get; set; }
        public string Reason { get; set; }
        public string ReasonSC { get; set; }
        public Message Message { get; set; }
        public bool IsLikelyAbnormalBet { get; set; }
        public bool IsAbnormalAccount { get; set; }
    }
}