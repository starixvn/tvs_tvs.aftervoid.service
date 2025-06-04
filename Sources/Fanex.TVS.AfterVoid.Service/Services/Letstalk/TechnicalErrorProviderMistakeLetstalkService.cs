using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fanex.LetstalkService.Client;
using Fanex.LetstalkService.Client.Models;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface TechnicalErrorProviderMistakeLetstalkService
{
    bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
}

public class TechnicalErrorProviderMistakeLetstalkServiceImp(LetstalkClientService letstalkClientService) : TechnicalErrorProviderMistakeLetstalkService
{
    private const int Currency20 = 20;
    public bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
    {

        UpdateNotificationLetstalkModel(tickets, model);
        if (model.TotalTicketsCount == 0)
        {
            return true;
        }

        var content = CreateMessageContent(model);

        var letstalkModel = CreateLetstalkModel(content);
        var isSentSuccessfully = letstalkClientService.SendMessage(letstalkModel);
        return isSentSuccessfully;


    }
    private static void UpdateNotificationLetstalkModel(IEnumerable<TicketEmailModel> tickets, NotificationLetstalkModel model)
    {
        model.TotalTicketsCount = tickets.Count(IsValidTicketForSendLetstalk);
    }

    private static bool IsValidTicketForSendLetstalk(TicketEmailModel ticket)
    {
        return !(AppSettingService.AppSetting.LetstalkConfig.IsProdEnv && ticket.Currency != Currency20);
    }
    private static LetstalkModel CreateLetstalkModel(string content)
    {
        return new LetstalkModel
        {
            TargetName = "oneback-os-bc-auto-void",
            Subject = "",
            Content = content,
            Group = ""
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