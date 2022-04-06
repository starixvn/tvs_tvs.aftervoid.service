using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Criteria
{
    public class MailingListCriteria : CriteriaBase
    {
        public int? CategoryId { get; set; }

        public override string GetSettingKey() => "SpecialDelete_MailingList_Select";

        public override bool IsValid() => true;
    }
}