namespace TicketVoidingSystem.Models;

public class TemplateModel
{
    public int TemplateId { get; set; }
    public string MsgEnglish { get; set; }
    public string MsgChineseTraditional { get; set; }
    public string MsgChineseSimplified { get; set; }
    public string MsgChineseNewSimplified { get; set; }
    public string JsonValue { get; set; }
}

public class TemplateSetting
{
    public int TemplateId { get; set; }
    public int SubIssueTypeId { get; set; }
    public int? SportType { get; set; }
    public bool? IsCheckedVar { get; set; }
}