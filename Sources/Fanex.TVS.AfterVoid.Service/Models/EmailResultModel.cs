namespace Fanex.TVS.AfterVoid.Service.Models;

public class EmailResultModel
{
    public int VoidingHistoryId { get; set; }

    public string Message { get; set; }

    public string Licensees { get; set; }

    public int Type { get; set; }
}