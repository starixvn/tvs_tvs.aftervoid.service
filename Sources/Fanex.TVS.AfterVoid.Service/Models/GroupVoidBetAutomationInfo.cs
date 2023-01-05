using System.Collections.Generic;
using System.Linq;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class GroupVoidBetAutomationInfo
    {
        public int RequestId { get; set; }

        public int MatchId => VoidBetAutomationList.First().MatchId;

        public bool? IsSentLetsTalk => VoidBetAutomationList.First().IsSentLetsTalk;

        public bool IsInsert => VoidBetAutomationList.First().IsInsert;

        public string BroadcastSessionId => VoidBetAutomationList.First().BroadcastSessionId;

        public bool IsAutoVoid => VoidBetAutomationList.First().IsAutoVoid;

        public IEnumerable<VoidBetAutomationInfo> VoidBetAutomationList { get; set; }

        public RequestDetailViewModel RequestInfo { get; set; }
    }
}