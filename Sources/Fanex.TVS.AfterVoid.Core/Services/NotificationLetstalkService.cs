using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Core.Models;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Extensions;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface NotificationLetstalkService
    {
        bool SendToSettledBetGroup(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);

        Task<bool> SendToAbnormalBetGroup(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
    }

    public class NotificationLetstalkServiceImp : NotificationLetstalkService
    {
        private readonly LetstalkClientService letstalkService;
        private readonly AfterVoidEmailService afterVoidEmailService;
        private const int maxContent = 1024;
        private const int currency20 = 20;

        public NotificationLetstalkServiceImp(LetstalkClientService letstalkService, AfterVoidEmailService afterVoidEmailService)
        {
            this.letstalkService = letstalkService;
            this.afterVoidEmailService = afterVoidEmailService;
        }

        public bool SendToSettledBetGroup(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
        {
            if (!tickets.Any())
            {
                return true;
            }

            UpdateNotificationLetstalkModel(tickets, model);
            var contents = model.IsMemberTemplate ? CreateMessageContentWithMemberTemplate(tickets, model) : CreateMessageContent(model);
            if (contents.Count == 0)
            {
                return true;
            }
            foreach (var item in contents)
            {
                var letstalkModel = CreateLetstalkModelSettledBet(item);
                var isSentSuccessfully = letstalkService.SendMessage(letstalkModel);
                if (!isSentSuccessfully)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> SendToAbnormalBetGroup(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
        {
            if (!tickets.Any())
            {
                return true;
            }

            UpdateNotificationLetstalkModel(tickets, model);
            var letstalkModel = await CreateLetstalkModelAbnormalBet(model);
            var isSentSuccessfully = letstalkService.SendMessage(letstalkModel);
            if (!isSentSuccessfully)
            {
                return false;
            }

            return true;
        }

        private static List<string> CreateMessageContent(NotificationLetstalkModel model)
        {
            if (model.SingleCount == 0 && model.ParlayCount == 0 && model.CashOutCount == 0)
            {
                return new List<string>();
            }
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
            if (model.SingleCount == 0 && model.ParlayCount == 0 && model.CashOutCount == 0)
            {
                return new List<string>();
            }
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

        private static LetstalkModel CreateLetstalkModelSettledBet(string content)
        {
            return new()
            {
                TargetName = "Special-Delete",
                Subject = "Void Settled Bet Notification",
                Content = content,
                Group = AppConfigService.AppConfig.LetstalkSettledBetGroup
            };
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

        private static string CreateMessageAbnormalBetContent(NotificationLetstalkModel model)
        {
            if (model.SingleCount == 0 && model.ParlayCount == 0 && model.CashOutCount == 0)
            {
                return string.Empty;
            }

            switch (model.IssueTypeId)
            {
                case IssueTypeConstant.AbnormalBet when model.FilterBy == null:
                case IssueTypeConstant.AbnormalBetRealMember:
                    return CreateMessageContentWithMatch(model);

                case IssueTypeConstant.AbnormalBet when model.FilterBy == 1:
                    return CreateMessageContentWithNoneMatch(model);

                default:
                    return string.Empty;
            }
        }

        private static string CreateMessageContentWithMatch(NotificationLetstalkModel model)
        {
            var content = new StringBuilder();
            content.AppendLine($"Request ID: {model.RequestId}");
            content.AppendLine($"Match ID: {model.Match.MatchId}");
            content.AppendLine($"Event Date: {model.Match.EventDate:dd MMMM yyyy}");
            content.AppendLine($"Kick Off: {model.Match.KickOffTime:h:mm tt}");
            content.AppendLine($"League: {model.Match.LeagueName}");
            content.AppendLine($"Match: {model.Match.MatchEvent}");
            if (!string.IsNullOrEmpty(model.DecidedBy))
            {
                content.AppendLine($"Decided by: {model.DecidedBy}");
            }
            content.AppendLine($"Requested by: {model.RequestedBy}");
            content.AppendLine($"Voided by: {model.UserName}");

            return content.ToString();
        }

        private static string CreateMessageContentWithNoneMatch(NotificationLetstalkModel model)
        {
            var content = new StringBuilder();
            content.AppendLine($"Request ID: {model.RequestId}");
            if (!string.IsNullOrEmpty(model.DecidedBy))
            {
                content.AppendLine($"Decided by: {model.DecidedBy}");
            }
            content.AppendLine($"Requested by: {model.RequestedBy}");
            content.AppendLine($"Voided by: {model.UserName}");

            return content.ToString();
        }

        private async Task<LetstalkModel> CreateLetstalkModelAbnormalBet(NotificationLetstalkModel model)
        {
            var subject = GetSubjectLetstalkModel(model);
            var content = CreateMessageAbnormalBetContent(model);
            var attachments = await afterVoidEmailService.GetContentExcelVoidedTicket(model.RequestId,
                model.Match?.MatchId ?? 29, model.IssueTypeId);
            return new LetstalkModel
            {
                TargetName = "TVS-Void-Abnormal-Bets",
                Subject = subject,
                Content = content,
                Group = AppConfigService.AppConfig.LetstalkAbnormalBetGroup,
                LetstalkAttachments = new List<LetstalkAttachment>
                {
                    new()
                    {
                        FileName = $"Req.{model.RequestId}_TVS_DeleteBet.xlsx",
                        Bytes = attachments
                    }
                }
            };
        }

        private static string GetSubjectLetstalkModel(NotificationLetstalkModel model)
        {
            return model.IssueTypeId switch
            {
                IssueTypeConstant.AbnormalBet when model.FilterBy == null => "Void Abnormal Bet Notification",
                IssueTypeConstant.AbnormalBetRealMember => "Void AB (Real Member) Notification",
                IssueTypeConstant.AbnormalBet when model.FilterBy == 1 => "Void Abnormal Bet Notification - Mix Parlay",
                _ => string.Empty
            };
        }

        private static void UpdateNotificationLetstalkModel(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
        {
            model.SingleCount = tickets.Where(IsValidSettledSingleTicket).Count(ticket => !ticket.IsInstantSettlementTicket);
            model.CashOutCount = tickets.Count(ticket => ticket.IsCashOut);
            model.InstantOUCount = tickets.Where(IsValidSettledSingleTicket).Count(ticket => ticket.IsInstantSettlementTicket);
            model.ParlayCount = tickets.Count(ticket => IsValidSettledParlayTicket(ticket, false));
            model.OutstandingSingleCount = tickets.Count(ticket => !ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
            model.OutstandingParlayCount = tickets.Count(ticket => ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
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
    }
}