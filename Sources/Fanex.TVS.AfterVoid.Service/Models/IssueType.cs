namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class IssueType
    {
        public int IssueTypeId { get; set; }
        public string IssueTypeName { get; set; }
    }

    public class SubIssueType
    {
        public int SubIssueTypeId { get; set; }
        public int IssueTypeId { get; set; }
        public string SubIssueTypeName { get; set; }
        public string SubIssueTypeNameCS { get; set; }
    }

    public class IssueInfo : SubIssueType
    {
        public string Name { get; set; }
    }
}