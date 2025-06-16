using Fanex.Data.Repository;
using TicketVoidingSystem.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class GetTicketsRecoverEmailCriteria : CriteriaBase
{
    public int SiteDbGroupId { get; set; }
    public string SingleTransIds { get; set; }
    public string SubParlayTransIds { get; set; }
    public bool IsITSupport { get; set; }
    public bool IsITSupportCreatedRequest { get; set; }
    public bool IsInsertRoutineTask { get; set; }

    public override string GetSettingKey() => Helper.GetSettingKeyBySiteDbGroupId(SiteDbGroupId, "TicketVoiding_Email_RecoveredTicket_Select");

    public override bool IsValid() => true;
}