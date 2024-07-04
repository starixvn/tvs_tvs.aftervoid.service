using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class CreateMessageStageParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public Match MatchInfo { get; set; }
    public VoidDataXmlModel VoidDataXml { get; set; }
    public IEnumerable<TicketEmailModel> NormalTickets { get; set; }
    public IEnumerable<TicketEmailModel> SpecialTickets { get; set; }
}