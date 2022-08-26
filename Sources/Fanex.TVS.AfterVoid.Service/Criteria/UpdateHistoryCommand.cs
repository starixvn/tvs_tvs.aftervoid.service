using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class UpdateHistoryCommand : NonQueryCommand
    {
        public int VoidingHistoryId { get; set; }
        public byte StatusId { get; set; }
        public int MessageId { get; set; }
        public int ReasonId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_VoidingHistory_Update";

        public override bool IsValid() => true;
    }
}