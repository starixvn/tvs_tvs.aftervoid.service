using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetTicketsEmailCriteria : CriteriaBase
    {
        public int VoidingHistoryId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Email_VoidedTicket_Select";

        public override bool IsValid() => true;
    }
}