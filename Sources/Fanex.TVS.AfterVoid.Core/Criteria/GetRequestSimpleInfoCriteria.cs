using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetRequestSimpleInfoCriteria : CriteriaBase
    {
        public int RequestId { get; set; }
        public int VoidingHistoryId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_RequestInfo_Select";

        public override bool IsValid() => true;
    }
}