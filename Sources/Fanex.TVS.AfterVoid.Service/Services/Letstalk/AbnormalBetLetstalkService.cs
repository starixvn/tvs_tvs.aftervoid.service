using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Core.Models;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface AbnormalBetLetstalkService
{
    Task<bool> Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
}

public class AbnormalBetLetstalkServiceImp : AbnormalBetLetstalkService
{
    private readonly SendLetstalkServiceStage letstalkServiceStage;
    private readonly LetstalkClientService letstalkClientService;

    private const int currency20 = 20;

    public AbnormalBetLetstalkServiceImp(SendLetstalkServiceStage letstalkServiceStage,
        LetstalkClientService letstalkClientService)
    {
        this.letstalkServiceStage = letstalkServiceStage;
        this.letstalkClientService = letstalkClientService;
    }

    public async Task<bool> Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
    {
        var creditTickets = tickets.Where(x => x.IsCredit);
        if (!creditTickets.Any())
        {
            return true;
        }

        UpdateNotificationLetstalkModel(creditTickets, model);
        var letstalkModel = await CreateLetstalkModelAbnormalBet(model);
        if (string.IsNullOrWhiteSpace(letstalkModel.Content))
        {
            return true;
        }
        var isSentSuccessfully = letstalkClientService.SendMessage(letstalkModel);
        if (!isSentSuccessfully)
        {
            return false;
        }

        return true;
    }

    private async Task<LetstalkModel> CreateLetstalkModelAbnormalBet(NotificationLetstalkModel model)
    {
        var subject = GetSubjectLetstalkModel(model);
        var content = CreateMessageAbnormalBetContent(model);
        var attachments = await letstalkServiceStage.GetContentExcelVoidedTicket(model.RequestId, model.Match?.MatchId ?? 29,
            model.IssueTypeId, model.VoidingHistoryIds);
        return new LetstalkModel
        {
            TargetName = "Ticket-Voiding-System",
            Subject = subject,
            Content = content,
            Group = AppSettingService.AppSetting.LetstalkConfig.VoidAbnormalBets,
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

    private static string CreateMessageAbnormalBetContent(NotificationLetstalkModel model)
    {
        switch (model.IssueTypeId)
        {
            case IssueTypeConstant.AbnormalBet when model.FilterBy == null:
            case IssueTypeConstant.AbnormalBetRealMember:
                return CreateMessageContentAbnormalBetWithMatch(model);

            case IssueTypeConstant.AbnormalBet when model.FilterBy == 1:
                return CreateMessageContentAbnormalBetWithNoneMatch(model);

            default:
                return string.Empty;
        }
    }

    private static string CreateMessageContentAbnormalBetWithMatch(NotificationLetstalkModel model)
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

    private static string CreateMessageContentAbnormalBetWithNoneMatch(NotificationLetstalkModel model)
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

    private static void UpdateNotificationLetstalkModel(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
    {
        model.SingleCount = tickets.Where(IsValidSingleTicketAbnormalBetGroup).Count(ticket => !ticket.IsInstantSettlementTicket);
        model.InstantOUCount = tickets.Where(IsValidSingleTicketAbnormalBetGroup).Count(ticket => ticket.IsInstantSettlementTicket);
        model.ParlayCount = tickets.Count(IsValidParlayTicketAbnormalBetGroup);
        model.OutstandingSingleCount = tickets.Count(ticket => !ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
        model.OutstandingParlayCount = tickets.Count(ticket => ticket.BetTypeId.IsParlay() && ticket.Status.IsRunningOrWaiting());
    }

    private static bool IsValidSingleTicketAbnormalBetGroup(TicketEmailModel ticket)
    {
        var result = !ticket.BetTypeId.IsParlay() && !ticket.IsCashOut;
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return result && ticket.Currency != currency20;
        }

        return result;
    }

    private static bool IsValidParlayTicketAbnormalBetGroup(TicketEmailModel ticket)
    {
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return ticket.BetTypeId.IsParlay() && ticket.Currency != currency20;
        }

        return ticket.BetTypeId.IsParlay();
    }
}