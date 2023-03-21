using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class GetReasonNameCriteria : CriteriaBase
{
    public int SubIssueTypeId { get; set; }
    public int SportType { get; set; }

    public override string GetSettingKey() => "TicketVoiding_ReasonTemplate_Select";

    public override bool IsValid() => true;
}