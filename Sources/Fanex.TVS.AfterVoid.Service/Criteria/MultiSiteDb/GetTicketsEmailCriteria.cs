using Fanex.Data.Repository;
using TicketVoidingSystem.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb
{
    public class GetTicketsEmailCriteria : CriteriaBase
    {
        public int SiteDbGroupId { get; set; }
        public int VoidingHistoryId { get; set; }
        public bool IsITSupport { get; set; }
        public bool IsITSupportCreatedRequest { get; set; }

        public override string GetSettingKey() => Helper.GetSettingKeyBySiteDbGroupId(SiteDbGroupId, "TicketVoiding_Email_VoidedTicket_Select");

        public override bool IsValid() => true;
    }
}