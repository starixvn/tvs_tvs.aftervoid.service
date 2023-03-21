using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria;

public class GetVoidingHistoryCriteria : CriteriaBase
{
    public string VoidingHistoryIds { get; set; }

    public override string GetSettingKey() => "TicketVoiding_VoidingHistory_Select";

    public override bool IsValid() => true;
}