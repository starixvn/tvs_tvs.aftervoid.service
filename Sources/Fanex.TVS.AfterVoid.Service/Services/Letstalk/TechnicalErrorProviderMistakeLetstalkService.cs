using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Client.Models;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface TechnicalErrorProviderMistakeLetstalkService
{
    bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
}

public class TechnicalErrorProviderMistakeLetstalkServiceImp(LetstalkClientService letstalkClientService) : TechnicalErrorProviderMistakeLetstalkService
{
    public bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
    {
        UpdateNotificationLetstalkModel(tickets, model);
        if (model.TotalTicketsCount == 0)
        {
            return true;
        }
        var isHasUusTicket = tickets.Any(ticket => ticket.Currency == CurrencyConstant.UusId);
        var content = CreateMessageContent(model);
        var letstalkModel = CreateLetstalkModel(content, isHasUusTicket);
        var isSentSuccessfully = letstalkClientService.SendMessage(letstalkModel);
        return isSentSuccessfully;
    }

    private static void UpdateNotificationLetstalkModel(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
    {
        model.TotalTicketsCount = tickets.Count();
        model.IsHasUusTicket = tickets.Any(ticket => ticket.Currency is CurrencyConstant.UusId);
    }

    private static LetstalkModel CreateLetstalkModel(string content, bool isHasUusTicket)
    {
        if (AppSettingService.AppSetting.LetstalkConfig.IsProdEnv)
        {
            return new LetstalkModel
            {
                TargetName = AppSettingService.AppSetting.LetstalkConfig.TargetName.BCAutoVoid,
                Subject = "",
                Content = content,
                Group = ""
            };
        }

        return new LetstalkModel
        {
            TargetName = AppSettingService.AppSetting.LetstalkConfig.TargetName.TicketVoidingSystem,
            Subject = "BC Auto Void",
            Content = content,
            Group = GroupFactory.GetGroup(LetstalkGroupType.VoidAbnormalBets, isHasUusTicket)
        };
    }

    private static string CreateMessageContent(NotificationLetstalkModel model)
    {
        var content = new StringBuilder();
        content.AppendLine($"[{model.Match.SportTypeName}]");
        content.AppendLine($"TVS Request NO : {model.RequestId}");
        content.AppendLine($"Match ID: {model.Match.MatchId}");
        content.AppendLine($"Event Date: {model.Match.EventDate:dd MMMM yyyy}");
        content.AppendLine($"Kick Off: {model.Match.KickOffTime:h:mm tt}");
        content.AppendLine($"League: {model.Match.LeagueName}");
        content.AppendLine($"Match: {model.Match.MatchEvent}");
        content.AppendLine("");
        content.AppendLine("Void Reason: BC request VOID");
        content.AppendLine($"Total ticket: {model.TotalTicketsCount}");
        content.Append($"Void Time: {model.VoidFromTime} - {model.VoidToTime}");

        return content.ToString();
    }
}