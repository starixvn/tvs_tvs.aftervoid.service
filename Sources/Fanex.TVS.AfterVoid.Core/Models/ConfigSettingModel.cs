using ONEbook.UM.Configurations;

namespace Fanex.TVS.AfterVoid.Core.Models
{
    public class ConfigSettingModel
    {
        public bool OnProcessing { get; set; }

        public string ServiceName { get; set; }

        public string SentryClientKey { get; set; }

        public string CategorySentryLog { get; set; }

        public UMClientConfiguration UMConfig { get; set; }
    }
}