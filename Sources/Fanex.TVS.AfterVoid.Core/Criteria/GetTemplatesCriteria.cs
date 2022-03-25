using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetTemplatesCriteria : CriteriaBase
    {
        public override string GetSettingKey() => "TicketVoiding_MessageTemplate_Select";

        public override bool IsValid() => true;
    }
}