using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class GetMessageCriteria : CriteriaBase
    {
        public int MessageId { get; set; }

        public override string GetSettingKey() => "NAP_SpecialDelete_Message_Select";

        public override bool IsValid() => MessageId > 0;
    }
}