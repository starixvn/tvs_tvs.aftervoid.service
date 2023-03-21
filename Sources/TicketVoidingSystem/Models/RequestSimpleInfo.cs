namespace TicketVoidingSystem.Models;

public class RequestSimpleInfo
{
    public int MatchId { get; set; }
    public string LastPerformedBy { get; set; }
    public int TotalVoidedSingle { get; set; }
    public int TotalVoidedParlay { get; set; }
}