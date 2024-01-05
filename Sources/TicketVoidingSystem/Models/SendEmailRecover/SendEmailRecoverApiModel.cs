using System.Collections.Generic;

namespace TicketVoidingSystem.Models.SendEmailRecover;

public class SendEmailRecoverApiModel
{
    public IEnumerable<TicketEmailModel> EmailRecoverTickets { get; set; }
    public string NegativeBalanceUsers { get; set; }
}