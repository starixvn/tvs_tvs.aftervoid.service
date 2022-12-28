using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetAllVoidBetAutomationInfoCriteria : CriteriaBase
    {
        public byte Mode { get; set; }

        public override string GetSettingKey() => "TicketVoiding_VoidBetAutomation_Service_SelectAll";

        public override bool IsValid() => true;
    }
}