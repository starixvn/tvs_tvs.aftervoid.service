using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class InsertVoidedTransactionCommand : NonQueryCommand
    {
        public string TransIds { get; set; }
        public int MessageId { get; set; }
        public int ReasonId { get; set; }
        public int MatchId { get; set; }
        public int? IssueTypeId { get; set; }
        public int? SubIssueTypeId { get; set; }
        public string XmlCustInfo { get; set; }

        public override string GetSettingKey() => "TicketVoiding_VoidedTransaction_Insert";

        public override bool IsValid() => !string.IsNullOrWhiteSpace(XmlCustInfo);
    }
}