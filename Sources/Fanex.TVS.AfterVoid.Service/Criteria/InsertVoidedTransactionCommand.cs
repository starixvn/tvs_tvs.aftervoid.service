using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class InsertVoidedTransactionCommand : NonQueryCommand
{
    public int MessageId { get; set; }
    public int ReasonId { get; set; }
    public int? IssueTypeId { get; set; }
    public int? SubIssueTypeId { get; set; }
    public string TicketsXml { get; set; }

    public override string GetSettingKey() => "TicketVoiding_VoidedTransaction_Insert";

    public override bool IsValid() => !string.IsNullOrWhiteSpace(TicketsXml);
}