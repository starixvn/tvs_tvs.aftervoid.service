using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class GetMatchInfoCriteria : CriteriaBase
    {
        public int MatchId { get; set; }

        public long TransId { get; set; }

        public bool IsMatch3rd { get; set; }

        public override string GetSettingKey() => "NAP_SpecialDelete_MatchInfo_Select";

        public override bool IsValid() => MatchId > 0 || TransId > 0;
    }
}