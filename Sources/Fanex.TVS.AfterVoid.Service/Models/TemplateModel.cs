namespace Fanex.TVS.AfterVoid.Service.Models;

public class TemplateContent
{
    public string MsgEnglish { get; set; }
    public string MsgChineseTraditional { get; set; }
    public string MsgChineseSimplified { get; set; }
    public string MsgChineseNewSimplified { get; set; }
}

public class TemplateModel : TemplateContent
{
    public int TemplateId { get; set; }
    public string JsonValue { get; set; }
}

public class InputText : TemplateContent
{
    public string Name { get; set; }
    public string Pattern { get; set; }
}