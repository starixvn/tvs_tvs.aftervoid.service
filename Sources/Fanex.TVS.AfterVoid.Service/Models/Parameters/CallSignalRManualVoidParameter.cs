using System.Collections.Generic;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Models.Parameters;

public class CallSignalRManualVoidParameter
{
    public VoidBetAutomationInfo AutomationInfo { get; set; }
    public bool IsLastedInRequest { get; set; }
    public string CurrentRequestStatus { get; set; }
    public string CreatorName { get; set; }
    public IEnumerable<Result> CurrentEmailResults { get; set; }
    public IEnumerable<EmailResultModel> EmailResultsHistory { get; set; }
}