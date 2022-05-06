using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Core.Models;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface NotificationLetstalkService
    {
        bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
    }

    public class NotificationLetstalkServiceImp : NotificationLetstalkService
    {
        private readonly LetstalkClientService letstalkService;
        private const int maxContent = 1024;
        private const int currency20 = 20;

        public NotificationLetstalkServiceImp(LetstalkClientService letstalkService)
        {
            this.letstalkService = letstalkService;
        }

        public bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
        {
            if (!tickets.Any())
            {
                return true;
            }
            var contents = model.IsMemberTemplate ? CreateMessageContentWithMemberTemplate(tickets, model) : CreateMessageContent(tickets, model);
            if (contents.Count == 0)
            {
                return true;
            }
            foreach (var item in contents)
            {
                var letstalkModel = CreateLetstalkModel(item);
                var isSentSuccessfully = letstalkService.SendMessage(letstalkModel);
                if (!isSentSuccessfully)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<string> CreateMessageContent(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
        {
            var singleTickets = tickets.Where(ticket => !ticket.IsMixParlay && IsValidSettledSingleTicket(ticket));
            var singleCount = singleTickets.Count(ticket => !ticket.IsInstantSettlementTicket);
            var cashOutCount = tickets.Count(ticket => ticket.IsCashOut);
            var instantOUCount = singleTickets.Count(ticket => ticket.IsInstantSettlementTicket);
            var parlayCount = tickets.Count(ticket => IsValidSettledParlayTicket(ticket, true));
            if (singleCount == 0 && parlayCount == 0 && cashOutCount == 0)
            {
                return new List<string>();
            }
            model.SingleCount = singleCount;
            model.CashOutCount = cashOutCount;
            model.InstantOUCount = instantOUCount;
            model.ParlayCount = parlayCount;
            StringBuilder content = new StringBuilder();
            content.AppendLine($"Match ID: {model.Match.MatchId}");
            content.AppendLine($"Event Date: {model.Match.EventDate.ToString("dd MMMM yyyy")}");
            content.AppendLine($"Kick Off: {model.Match.KickOffTime.ToString("h:mm tt")}");
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
            var singleTickets = tickets.Where(IsValidSettledSingleTicket);
            var singleCount = singleTickets.Count(ticket => !ticket.IsInstantSettlementTicket);
            var cashOutCount = tickets.Count(ticket => ticket.IsCashOut);
            var instantOUCount = singleTickets.Count(ticket => ticket.IsInstantSettlementTicket);
            var parlayCount = tickets.Count(ticket => IsValidSettledParlayTicket(ticket, false));
            if (singleCount == 0 && parlayCount == 0 && cashOutCount == 0)
            {
                return new List<string>();
            }
            model.SingleCount = singleCount;
            model.CashOutCount = cashOutCount;
            model.InstantOUCount = instantOUCount;
            model.ParlayCount = parlayCount;
            model.OutstandingSingleCount = tickets.Count(ticket => !ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
            model.OutstandingParlayCount = tickets.Count(ticket => ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
            var members = tickets.Where(ticket => ticket.Status.IsSettled()).Select(item => item.UserName).Distinct();
            var userNames = $"Username: {string.Join(", ", members)}";

            StringBuilder content = new StringBuilder();
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

        private static LetstalkModel CreateLetstalkModel(string content)
        {
            return new()
            {
                TargetName = "Special-Delete",
                Subject = "Void Settled Bet Notification",
                Content = content,
                Group = AppConfigService.AppConfig.LetstalkGroup
            };
        }

        private static bool IsValidSettledSingleTicket(TicketEmailModel ticket)
        {
            var result = !ticket.BetTypeId.IsParlay()
                        && !ticket.IsCashOut
                        && ticket.Status.IsSettled();
            if (AppConfigService.AppConfig.IsPRODEnvironment)
            {
                return result && ticket.Currency != currency20;
            }
            return result;
        }

        private static bool IsValidSettledParlayTicket(TicketEmailModel ticket, bool isCheckSubParlay)
        {
            var result = isCheckSubParlay ? ticket.IsMixParlay && ticket.SubStatus.IsSettled()
                                    : ticket.BetTypeId.IsParlay() && ticket.Status.IsSettled();
            if (AppConfigService.AppConfig.IsPRODEnvironment)
            {
                return result && ticket.Currency != currency20;
            }
            return result;
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
    }
}