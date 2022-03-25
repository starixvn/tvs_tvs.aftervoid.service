//using Fanex.TVS.AfterVoid.Core.Models;

//namespace Fanex.TVS.AfterVoid.Core.SendEmail.Models
//{
//    using Resources;

//    public class MailRecordResult
//    {
//        public SiteEmail SiteEmail { get; set; }

//        public string Subject { get; set; }

//        public string EmailBody { get; set; }

//        public MailRecord MailRecord => new MailRecord
//        {
//            MailTo = SiteEmail.ToList,
//            MailCc = SiteEmail.CcList,
//            MailBcc = SiteEmail.BccList,
//            Title = Labels.EmailFrom,
//            Subject = Subject
//            .Replace("[IBO]", "[KZING]")
//            .Replace("[BODOG]", "[HAYDOCK]")
//            .Replace("[AVIA]", "[ORIENTAL GAME]"),
//            Body = EmailBody
//        };
//    }
//}