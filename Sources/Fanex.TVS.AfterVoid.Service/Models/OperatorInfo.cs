using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class OperatorQuery
    {
        public int OperatorId { get; set; }
        public byte OperatorType { get; set; }
    }

    public class OperatorInfo : OperatorQuery
    {
        public int MatchId { get; set; }
        public string UserName { get; set; }

        public string GroupName => OperatorType == OperatorConstant.TraderType ?
            OperatorConstant.TraderName : OperatorConstant.SuspenderName;
    }

    public class OperatorInfoModel
    {
        public int MatchId { get; set; }

        public int TraderID { get; set; }

        public string TraderName { get; set; }

        public int SuspenderID { get; set; }

        public string SuspenderName { get; set; }
    }
}