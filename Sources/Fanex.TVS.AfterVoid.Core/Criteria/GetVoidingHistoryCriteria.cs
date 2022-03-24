using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetVoidingHistoryCriteria : CriteriaBase
    {
        public int VoidingHistoryId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_VoidingHistory_Select";

        public override bool IsValid() => true;
    }
}