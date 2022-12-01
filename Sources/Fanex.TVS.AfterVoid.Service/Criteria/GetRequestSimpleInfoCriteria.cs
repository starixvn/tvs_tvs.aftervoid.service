using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetRequestSimpleInfoCriteria : CriteriaBase
    {
        public int RequestId { get; set; }

        public int MatchId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_RequestInfo_Select";

        public override bool IsValid() => RequestId > 0;
    }
}