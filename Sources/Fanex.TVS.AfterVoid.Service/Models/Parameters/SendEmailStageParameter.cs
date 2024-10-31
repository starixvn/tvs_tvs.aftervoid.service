using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class SendEmailStageParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public Match MatchInfo { get; set; }
    public Message NormalMessage { get; set; }
    public Message SpecialMessage { get; set; }
    public VoidedReason VoidReason { get; set; }
    public IEnumerable<TicketEmailModel> EmailTickets { get; set; }
}