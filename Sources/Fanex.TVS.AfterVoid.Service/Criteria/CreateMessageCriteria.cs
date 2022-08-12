using Fanex.Data.Repository;

namespace Fanex.TVS.AfterVoid.Service.Criteria
{
    public class CreateMessageCriteria : CriteriaBase
    {
        public CreateMessageCriteria()
        {
            IsBatch = true;
            MsgType = 1;
        }

        public bool IsBatch { get; set; }

        public int MsgType { get; set; }

        public string Content_ch { get; set; }

        public string Content_cs { get; set; }

        public string Content_zhcn { get; set; }

        public string Content_en { get; set; }

        public string Content_es { get; set; }

        public string Content_it { get; set; }

        public string Content_jp { get; set; }

        public string Content_ko { get; set; }

        public string Content_th { get; set; }

        public int Creator { get; set; }

        public string CreatorName { get; set; }

        public bool IsSticky { get; }

        public bool IsTest { get; }

        public int MatchId { get; set; }

        public int? LeagueGroupId { get; set; }

        public int MsgContentsType { get; set; }

        public override string GetSettingKey() => "NAP_SpecialDelete_Message_Insert";

        public override bool IsValid() => true;
    }
}