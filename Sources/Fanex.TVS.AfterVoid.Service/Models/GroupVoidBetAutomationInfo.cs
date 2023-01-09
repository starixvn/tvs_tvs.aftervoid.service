using System.Collections.Generic;
using System.Linq;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class GroupVoidBetAutomationInfo
    {
        public GroupVoidBetAutomationInfo()
        {
            VoidBetAutomationList = new List<VoidBetAutomationInfo>();
        }

        public int RequestId { get; set; }

        public int MatchId => VoidBetAutomationList.FirstOrDefault()?.MatchId ?? 0;

        public bool? IsSentLetsTalk => VoidBetAutomationList.FirstOrDefault()?.IsSentLetsTalk;

        public bool IsInsert => VoidBetAutomationList.FirstOrDefault()?.IsInsert ?? false;

        public string BroadcastSessionId => VoidBetAutomationList.FirstOrDefault()?.BroadcastSessionId;

        public bool IsAutoVoid => VoidBetAutomationList.FirstOrDefault()?.IsAutoVoid ?? false;

        public IEnumerable<VoidBetAutomationInfo> VoidBetAutomationList { get; set; }

        public RequestDetailViewModel RequestInfo { get; set; }
    }
}