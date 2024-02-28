using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class InsertVoidedTransactionStageParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public IEnumerable<TicketEmailModel> NormalTickets { get; set; }
    public IEnumerable<TicketEmailModel> SpecialTickets { get; set; }
    public int NormalMessageId { get; set; }
    public int SpecialMessageId { get; set; }
    public int VoidReasonId { get; set; }
}