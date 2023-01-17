using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetAutomationInfoAfterVoidCriteria : CriteriaBase
    {
        public override string GetSettingKey() => "TicketVoiding_VoidBetAutomation_AfterVoid_Select";

        public override bool IsValid() => true;
    }
}