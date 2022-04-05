using ONEbook.UM.Configurations;

namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class ConfigSettingModel
    {
        public bool OnProcessing { get; set; }

        public string ServiceName { get; set; }

        public string SentryClientKey { get; set; }

        public string CategorySentryLog { get; set; }

        public string LetstalkGroup { get; set; }

        public string LetstalkServiceApi { get; set; }

        public bool IsPRODEnvironment { get; set; }

        public string TicketVoidingSystemUrl { get; set; }

        public string RedisServer { get; set; }
        public string RedisPort { get; set; }
        public string RedisPassword { get; set; }
        public string RedisAppName { get; set; }

        public UMClientConfiguration UMConfig { get; set; }
    }
}