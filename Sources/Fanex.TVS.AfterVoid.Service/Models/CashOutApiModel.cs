using System.Collections.Generic;

namespace Fanex.TVS.AfterVoid.Service.Models
{
    public class CashOutApiModel
    {
        public IEnumerable<VoidBetInfo> VoidBetInfos { get; set; }
    }

    public class VoidBetInfo
    {
        public string TransId { get; set; }

        public string CustId { get; set; }
    }
}