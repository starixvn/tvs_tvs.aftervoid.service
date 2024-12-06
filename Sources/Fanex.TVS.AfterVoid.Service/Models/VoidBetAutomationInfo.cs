namespace Fanex.TVS.AfterVoid.Service.Models;

public class VoidBetAutomationInfo
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public int VoidingHistoryId { get; set; }

    public int SiteDbGroupId { get; set; }

    public string BroadcastSessionId { get; set; }

    public int StatusId { get; set; }

    public int MatchId { get; set; }

    public int RequestDetailId { get; set; }

    public string NegativeBalanceUsers { get; set; }

    public bool IsAutoVoid { get; set; }

    public bool? IsRunExternalService { get; set; }

    public int? MessageId { get; set; }

    public int? MessageIdSpecial { get; set; }

    public int? ReasonId { get; set; }

    public bool? IsSentBatchMessage { get; set; }

    public bool? IsSentMessageToAdminSite { get; set; }

    public bool? IsInsertedVoidedTransaction { get; set; }

    public bool? IsUpdatedVoidingHistory { get; set; }

    public bool? IsSentEmail { get; set; }

    public string VoidResults { get; set; }

    public RequestDetailViewModel RequestInfo { get; set; }

    public bool? IsDeletedZzzBetTrans { get; set; }

    public bool? IsDeletedMessage { get; set; }

    public bool? IsDeletedVoidedTransaction { get; set; }
}