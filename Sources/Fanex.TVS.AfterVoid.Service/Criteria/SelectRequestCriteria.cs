using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class SelectRequestCriteria : CriteriaBase
{
    public int RequestId { get; set; }

    public override string GetSettingKey() => "TicketVoiding_Request_Select";

    public override bool IsValid() => true;
}