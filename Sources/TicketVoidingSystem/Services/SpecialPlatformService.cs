using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fanex.TVS.AfterVoid.Service.Constants;
using Force.DeepCloner;
using TicketVoidingSystem.Extensions;
using TicketVoidingSystem.Models;

namespace TicketVoidingSystem.Services;

public interface SpecialPlatformService
{
    (IEnumerable<TicketEmailModel> NormalGroup, IEnumerable<TicketEmailModel> SpecialPlatformGroup) GroupTicketSaba(IEnumerable<TicketEmailModel> tickets);

    Message RemoveSabaInMessageContent(Match match, Message message);
}

public class SpecialPlatformServiceImp : SpecialPlatformService
{
    private static bool IsTicketSpecialPlatform(TicketEmailModel ticket)
    {
        return ticket.LeagueGroupId.IsLeagueGroupIdSaBa() && (ticket.IsChinaView || ticket.SpecialPlatform >= SpecialPlatformConstant.JollySports);
    }

    public (IEnumerable<TicketEmailModel> NormalGroup, IEnumerable<TicketEmailModel> SpecialPlatformGroup) GroupTicketSaba(IEnumerable<TicketEmailModel> tickets)
    {
        var specialPlatformTickets = tickets.Where(IsTicketSpecialPlatform);
        var normalTickets = tickets.Except(specialPlatformTickets);

        return (normalTickets, specialPlatformTickets);
    }

    public Message RemoveSabaInMessageContent(Match match, Message message)
    {
        if (match == null)
        {
            return message;
        }

        var outputMessage = message.DeepClone();
        var leagueNameReplaced = match.OriginLeagueName.Replace("SABA ", "", StringComparison.InvariantCultureIgnoreCase);
        var leagueNameSimplifiedReplaced = match.OriginLeagueNameSimplified.Replace("沙巴", "", StringComparison.InvariantCultureIgnoreCase)
            .Replace("SABA ", "", StringComparison.InvariantCultureIgnoreCase);
        outputMessage.ContentEnglish = outputMessage.ContentEnglish.Replace(match.OriginLeagueName, leagueNameReplaced, StringComparison.InvariantCultureIgnoreCase);
        var serializedContentChineseSimplified = JsonSerializer.Serialize(outputMessage.ContentChineseSimplified);
        var deSerializedContentChineseSimplified = JsonSerializer.Deserialize<string>(serializedContentChineseSimplified);
        outputMessage.ContentChineseSimplified = deSerializedContentChineseSimplified.Replace(match.OriginLeagueNameSimplified,
            leagueNameSimplifiedReplaced, StringComparison.InvariantCultureIgnoreCase);
        return outputMessage;
    }
}