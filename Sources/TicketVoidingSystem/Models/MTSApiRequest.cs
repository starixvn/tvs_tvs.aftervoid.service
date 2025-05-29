namespace TicketVoidingSystem.Models
{
    public class MTSApiRequest
    {
        public long TransId { get; set; }
    }

    public class MTSApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
} 