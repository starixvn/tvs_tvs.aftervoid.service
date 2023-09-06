using System.Collections.Generic;

namespace TicketVoidingSystem.Models;

public class SendEmailApiModel
{
    public Match Match { get; set; }

    public VoidedReason VoidedReason { get; set; }

    public Message Message { get; set; }

    public int IssueTypeId { get; set; }

    public string NegativeBalanceUsers { get; set; }

    public int RequestId { get; set; }

    public int? FilterBy { get; set; }

    public int SportType { get; set; }

    public int VoidingHistoryId { get; set; }

    public IEnumerable<TicketEmailModel> EmailTickets { get; set; }
}