using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class UpdateVoidBetAutomationInfoCriteria : NonQueryCommand
{
    public int UpdateType { get; set; }

    public int? RequestId { get; set; }

    public int? VoidingHistoryId { get; set; }

    public int? StatusId { get; set; }

    public bool? IsRunExternalService { get; set; }

    public int? MessageId { get; set; }

    public int? MessageIdSpecial { get; set; }

    public int? ReasonId { get; set; }

    public bool? IsSentBatchMessage { get; set; }

    public bool? IsSentMessageToAdminSite { get; set; }

    public bool? IsInsertedVoidedTransaction { get; set; }

    public bool? IsUpdatedVoidingHistory { get; set; }

    public bool? IsSentEmail { get; set; }

    public bool? IsSentLetsTalk { get; set; }

    public bool? IsDeletedMessage { get; set; }

    public bool? IsDeletedVoidedTransaction { get; set; }

    public string VoidResults { get; set; }

    public override string GetSettingKey() => "TicketVoiding_VoidBetAutomation_Service_Upd";

    public override bool IsValid() => true;
}