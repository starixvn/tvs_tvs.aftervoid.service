namespace TicketVoidingSystem.Models
{
    public class MTSApiRequest
    {
        public long BetId { get; set; }
    }

    public class MTSApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
} 