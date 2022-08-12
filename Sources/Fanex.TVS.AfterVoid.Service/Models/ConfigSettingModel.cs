using ONEbook.UM.Configurations;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class ConfigSettingModel
    {
        public bool OnProcessing { get; set; }

        public string ServiceName { get; set; }

        public string SentryClientKey { get; set; }

        public string CategorySentryLog { get; set; }

        public string LetstalkSettledBetGroup { get; set; }

        public string LetstalkAbnormalBetGroup { get; set; }

        public string LetstalkServiceApi { get; set; }

        public bool IsPRODEnvironment { get; set; }

        public string TicketVoidingSystemUrl { get; set; }

        public string JackTimeCashOutUrl { get; set; }

        public string AppMailId { get; set; }
        public string AppMailAddress { get; set; }
        public string MailingList { get; set; }

        public string FanexRemotingRefServiceUrl { get; set; }

        public UMClientConfiguration UMConfig { get; set; }
    }
}