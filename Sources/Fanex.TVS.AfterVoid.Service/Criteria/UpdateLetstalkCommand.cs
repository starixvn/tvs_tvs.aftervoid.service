using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class UpdateLetstalkCommand : NonQueryCommand
    {
        public int RequestId { get; set; }

        public string BroadcastSessionId { get; set; }

        public int? StatusId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_Letstalk_Update";

        public override bool IsValid() => true;
    }
}