using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class UpdateLetstalkCommand : NonQueryCommand
    {
        public int Mode { get; set; }

        public int RequestId { get; set; }

        public string BroadcastSessionId { get; set; }

        public int? StatusId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_LetsTalk_Update";

        public override bool IsValid() => true;
    }
}