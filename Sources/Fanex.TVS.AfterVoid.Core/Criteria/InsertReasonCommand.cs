using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class InsertReasonCommand : NonQueryCommand
    {
        public string ReasonEnglish { get; set; }
        public string ReasonChineseSimplified { get; set; }
        public int ReasonId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Reason_Insert";

        public override bool IsValid() => true;
    }
}