using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class InsertMessageChatBotCommand : NonQueryCommand
    {
        public int MessageId { get; set; }

        public int RequestId { get; set; }

        public int? MatchId { get; set; }

        public int VoidingHistoryId { get; set; }

        public override string GetSettingKey() => "TicketVoiding_ChatBotMessage_Insert";

        public override bool IsValid() => VoidingHistoryId > 0;
    }
}