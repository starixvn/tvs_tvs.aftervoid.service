namespace TicketVoidingSystem.Models;

public class Result
{
    public int ErrorCode { get; set; } = 0;

    public string ErrorMsg { get; set; } = string.Empty;

    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMsg) || ErrorCode == 0 || ErrorCode == 235 || ErrorCode == 236 || ErrorCode == 238;

    public object Data { get; set; }
}