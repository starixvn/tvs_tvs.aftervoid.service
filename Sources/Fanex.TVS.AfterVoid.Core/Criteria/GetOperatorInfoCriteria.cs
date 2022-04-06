using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetOperatorInfoCriteria : CriteriaBase
    {
        public string AgentIdsXml { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Email_OperatorInfo_Select";

        public override bool IsValid() => true;
    }
}