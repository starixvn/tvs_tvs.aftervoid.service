namespace TicketVoidingSystem.Models;

public class RequestStatus
{
    public int RequestId { get; set; }

    public string Status { get; set; }

    public RequestSimpleInfo SimpleInfo { get; set; }
}