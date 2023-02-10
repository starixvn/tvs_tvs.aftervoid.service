using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class GetAutomationInfoLetstalkCriteria : CriteriaBase
{
    public override string GetSettingKey() => "TicketVoiding_VoidBetAutomation_Letstalk_Select";

    public override bool IsValid() => true;
}