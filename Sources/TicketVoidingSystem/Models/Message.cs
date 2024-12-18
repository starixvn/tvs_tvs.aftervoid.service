namespace TicketVoidingSystem.Models;

public class Message
{
    public int MessageId { get; set; }

    public string ContentChineseTraditional { get; set; }

    public string ContentChineseSimplified { get; set; }

    public string ContentNewChineseSimplified { get; set; }

    public string ContentEnglish { get; set; }

    public string ContentSpanish { get; set; }

    public string ContentItalian { get; set; }

    public string ContentJapanese { get; set; }

    public string ContentKorean { get; set; }

    public string ContentThai { get; set; }

    public int CreatorId { get; set; }

    public string CreatorName { get; set; }

    public Message Clone()
    {
        return (Message)MemberwiseClone();
    }
}