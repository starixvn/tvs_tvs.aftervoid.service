using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetSubIssueTypeCriteria : CriteriaBase
    {
        public override string GetSettingKey() => "TicketVoiding_MessageTemplate_SubIssueType_Select";

        public override bool IsValid() => true;
    }
}