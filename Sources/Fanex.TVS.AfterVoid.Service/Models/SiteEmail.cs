namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class SiteEmail
    {
        public int SiteId { get; set; }

        public string Site { get; set; }

        public string ToList { get; set; }

        public string CcList { get; set; }

        public string BccList { get; set; }

        public int IsAvailabe { get; set; }

        public bool IsChinaView { get; set; }
    }
}