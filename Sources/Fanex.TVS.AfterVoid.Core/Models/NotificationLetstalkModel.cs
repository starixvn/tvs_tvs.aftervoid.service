namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class NotificationLetstalkModel
    {
        public Match Match { get; set; }
        public string DecidedBy { get; set; }
        public string UserName { get; set; }
        public string VoidFromTime { get; set; }
        public string VoidToTime { get; set; }
        public string VoidFromDate { get; set; }
        public string VoidToDate { get; set; }
        public int SingleCount { get; set; }
        public int CashOutCount { get; set; }
        public int InstantOUCount { get; set; }
        public int ParlayCount { get; set; }
        public int OutstandingSingleCount { get; set; }
        public int OutstandingParlayCount { get; set; }
        public bool IsMemberTemplate { get; set; }
    }
}