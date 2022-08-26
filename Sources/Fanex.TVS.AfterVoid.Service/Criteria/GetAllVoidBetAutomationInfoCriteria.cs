using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetAllVoidBetAutomationInfoCriteria : CriteriaBase
    {
        public override string GetSettingKey() => "TicketVoiding_VoidBetAutomation_Service_SelectAll";

        public override bool IsValid() => true;
    }
}