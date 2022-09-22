using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class CheckRequestOfITUserCriteria : CriteriaBase
    {
        public int VoidingHistoryId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Email_CheckITUser";

        public override bool IsValid() => true;
    }
}