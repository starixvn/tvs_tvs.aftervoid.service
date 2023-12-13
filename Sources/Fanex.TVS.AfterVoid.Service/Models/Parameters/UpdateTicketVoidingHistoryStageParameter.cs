using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class UpdateTicketVoidingHistoryStageParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public int NormalMessageId { get; set; }
    public int SpecialMessageId { get; set; }
    public int VoidReasonId { get; set; }
    public IEnumerable<TicketEmailModel> EmailTickets { get; set; }
}