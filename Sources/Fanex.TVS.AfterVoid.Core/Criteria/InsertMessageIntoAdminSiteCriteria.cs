using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Core.Criteria
{
    public class InsertMessageIntoAdminSiteCriteria : NonQueryCommand
    {
        public string men { get; set; }
        public string mch { get; set; }
        public bool member { get; set; }
        public bool agent { get; set; }
        public bool ma { get; set; }
        public bool sma { get; set; }
        public int creator { get; set; }

        public override string GetSettingKey() => "Dot_Admin_message_new_insert";

        public override bool IsValid() => true;
    }
}