namespace TicketVoidingSystem.Models;

public class RequestSimpleInfo
{
    public int MatchId { get; set; }
    public string LastPerformedBy { get; set; }
    public int TotalSingleProcessed { get; set; }
    public int TotalParlayProcessed { get; set; }
    public int TotalCancelCashOutProcessed { get; set; }
    public int TotalResettleProcessed { get; set; }
}