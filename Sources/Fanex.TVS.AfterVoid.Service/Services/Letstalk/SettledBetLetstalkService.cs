using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Core.Models;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface SettledBetLetstalkService
{
    bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
}

public class SettledBetLetstalkServiceImp(LetstalkClientService letstalkClientService) : SettledBetLetstalkService
{
    private const int maxContent = 1024;
    private const int currency20 = 20;

    public bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
    {
        UpdateNotificationLetstalkModel(tickets, model);
        if (model.SingleCount == 0 && model.ParlayCount == 0 && model.CashOutCount == 0)
        {
            return true;
        }

        var contents = model.IsMemberTemplate ? CreateMessageContentWithMemberTemplate(tickets, model) : CreateMessageContent(model);
        foreach (var item in contents)
        {
            var letstalkModel = CreateLetstalkModelSettledBet(item);
            var isSentSuccessfully = letstalkClientService.SendMessage(letstalkModel);
            if (!isSentSuccessfully)
            {
                return false;
            }
        }

        return true;
    }

    private static LetstalkModel CreateLetstalkModelSettledBet(string content)
    {
        return new LetstalkModel
        {
            TargetName = "Ticket-Voiding-System",
            Subject = "Void Settled Bet Notification",
            Content = content,
            Group = AppSettingService.AppSetting.LetstalkConfig.VoidSettledBets
        };
    }

    private static List<string> CreateMessageContent(NotificationLetstalkModel model)
    {
        var content = new StringBuilder();
        content.AppendLine($"Match ID: {model.Match.MatchId}");
        content.AppendLine($"Event Date: {model.Match.EventDate:dd MMMM yyyy}");
        content.AppendLine($"Kick Off: {model.Match.KickOffTime:h:mm tt}");
        content.AppendLine($"League: {model.Match.LeagueName}");
        content.AppendLine($"Match: {model.Match.MatchEvent}");
        if (!string.IsNullOrEmpty(model.DecidedBy))
        {
            content.AppendLine($"Decided by: {model.DecidedBy}");
        }
        content.AppendLine($"Voided by: {model.UserName}");
        content.AppendLine($"Void Time: {model.VoidFromTime} - {model.VoidToTime}");
        content.Append($"(Single Count: {model.SingleCount}, Instant OU: {model.InstantOUCount}, Cash Out: {model.CashOutCount}, Parlay Count: {model.ParlayCount})");
        return new List<string> { content.ToString() };
    }

    private static List<string> CreateMessageContentWithMemberTemplate(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
    {
        var messages = new List<string>();
        var members = tickets.Where(ticket => ticket.Status.IsSettled()).Select(item => item.UserName).Distinct();
        var userNames = $"Username: {string.Join(", ", members)}";

        var content = new StringBuilder();
        content.AppendLine($"Void Date: {model.VoidFromDate} - {model.VoidToDate}");
        if (!string.IsNullOrEmpty(model.DecidedBy))
        {
            content.AppendLine($"Decided by: {model.DecidedBy}");
        }
        content.AppendLine($"Voided by: {model.UserName}");
        content.AppendLine($"(Settled ticket: Single Count: {model.SingleCount}, Instant OU: {model.InstantOUCount}, Cash Out: {model.CashOutCount}, Parlay Count: {model.ParlayCount}");
        content.AppendLine($"Outstanding: Single Count: {model.OutstandingSingleCount}, Parlay Count: {model.OutstandingParlayCount})");
        if ($"{content}{userNames}".Length > maxContent)
        {
            messages.AddRange(GenerateMultipleContent(content, members));
        }
        else
        {
            messages.Add($"{content}{userNames}");
        }
        return messages;
    }

    private static IEnumerable<string> GenerateMultipleContent(StringBuilder content, IEnumerable<string> members)
    {
        var results = new List<string>();
        content.AppendLine("Username (p1): ");
        var tempContent = content.ToString();
        var patch = 1;
        var index = 0;
        foreach (var member in members)
        {
            if ($"{tempContent}, {member}".Length > maxContent)
            {
                results.Add(tempContent);
                patch++;
                tempContent = $"Username (p{patch}): \r\n{member}";
            }
            else
            {
                tempContent = index == 0 ? $"{tempContent}{member}" : $"{tempContent}, {member}";
            }
            index++;
        }
        results.Add(tempContent);
        return results;
    }

    private static void UpdateNotificationLetstalkModel(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
    {
        var voidedTickets = tickets.Where(x => !x.IsCancelCashOut);
        model.SingleCount = voidedTickets.Where(IsValidSettledSingleTicket).Count(ticket => !ticket.IsInstantSettlementTicket);
        model.InstantOUCount = voidedTickets.Where(IsValidSettledSingleTicket).Count(ticket => ticket.IsInstantSettlementTicket);
        model.ParlayCount = voidedTickets.Count(ticket => IsValidSettledParlayTicket(ticket, !model.IsMemberTemplate));
        model.CashOutCount = voidedTickets.Count(IsValidCashOutTicket);
        model.OutstandingSingleCount = voidedTickets.Count(ticket => !ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
        model.OutstandingParlayCount = voidedTickets.Count(ticket => ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
    }

    private static bool IsValidSettledSingleTicket(TicketEmailModel ticket)
    {
        var result = !ticket.BetTypeId.IsParlay()
                     && !ticket.IsCashOut
                     && ticket.Status.IsSettled();
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return result && ticket.Currency != currency20;
        }

        return result;
    }

    private static bool IsValidSettledParlayTicket(TicketEmailModel ticket, bool isCheckSubParlay)
    {
        var result = isCheckSubParlay ? ticket.IsMixParlay && ticket.SubStatus.IsSettled()
            : ticket.BetTypeId.IsParlay() && ticket.Status.IsSettled();
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return result && ticket.Currency != currency20;
        }

        return result;
    }

    private static bool IsValidCashOutTicket(TicketEmailModel ticket)
    {
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return ticket.IsCashOut && ticket.Currency != currency20;
        }

        return ticket.IsCashOut;
    }
}