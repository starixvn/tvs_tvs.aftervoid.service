namespace TicketVoidingSystem.Models
{
    public class Result
    {
        public Result()
        {
            ErrorCode = 0;
            ErrorMsg = string.Empty;
        }

        public int ErrorCode { get; set; }

        public string ErrorMsg { get; set; }

        public bool IsSuccessful => string.IsNullOrEmpty(ErrorMsg) || ErrorCode == 0 || ErrorCode == 235 || ErrorCode == 236 || ErrorCode == 238;

        public object Data { get; set; }
    }
}