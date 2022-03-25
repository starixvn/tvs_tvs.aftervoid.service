using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetVoidedReasonCriteria : CriteriaBase
    {
        public int ReasonId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Reason_Select";

        public override bool IsValid() => true;
    }
}