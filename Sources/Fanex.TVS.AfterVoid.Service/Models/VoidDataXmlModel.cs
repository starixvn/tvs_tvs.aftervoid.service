namespace Fanex.TVS.AfterVoid.Service.Models;

public class VoidDataXmlModel
{
    public int IssueTypeId { get; set; }

    public int VoidingHistoryId { get; set; }

    public int SubIssueTypeId { get; set; }

    public string SubIssueTypeName { get; set; }

    public string SubIssueTypeNameCS { get; set; }

    public string Reason { get; set; }

    public string ReasonSC { get; set; }

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

    public string DecidedBy { get; set; }

    public string VoidFromTime { get; set; }

    public string VoidToTime { get; set; }

    public string VoidFromDate { get; set; }

    public string VoidToDate { get; set; }

    public bool IsHasSettledTicket { get; set; }

    public int TotalTicket { get; set; }

    public int IsRunAutoAfterVoid { get; set; }

    public string SingleTransIds { get; set; }
}