using System.Collections.Generic;

namespace TicketVoidingSystem.Models;

public class CashOutApiModel
{
    public IEnumerable<VoidBetInfo> VoidBetInfos { get; set; }
}

public class VoidBetInfo
{
    public string TransId { get; set; }

    public string CustId { get; set; }
}