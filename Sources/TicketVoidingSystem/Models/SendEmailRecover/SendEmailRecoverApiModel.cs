using System.Collections.Generic;

namespace TicketVoidingSystem.Models.SendEmailRecover;

public class SendEmailRecoverApiModel
{
    public string ActorUsername { get; set; }
    public string NegativeBalanceUsers { get; set; }
    public IEnumerable<TicketRecoverEmailModel> Tickets { get; set; }
}