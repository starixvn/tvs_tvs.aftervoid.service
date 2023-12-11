using Fanex.TVS.AfterVoid.Service.Constants;
using Force.DeepCloner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    public (IEnumerable<TicketEmailModel> NormalGroup, IEnumerable<TicketEmailModel> SpecialPlatformGroup) GroupTicketSaba(IEnumerable<TicketEmailModel> tickets)
    {
        var specialPlatformTickets = tickets.Where(x =>
            x.LeagueGroupId.IsLeagueGroupIdSaBa() && (x.IsChinaView || x.SpecialPlatform == SpecialPlatformConstant.JollySports));

        var normalTickets = tickets.Except(specialPlatformTickets);

        return (normalTickets, specialPlatformTickets);
    }

    public Message RemoveSabaInMessageContent(Match match, Message message)
    {
        var outputMessage = message.DeepClone();
        var leagueNameReplaced = match.OriginLeagueName.Replace("SABA ", "", StringComparison.InvariantCultureIgnoreCase);
        var leagueNameSimplifiedReplaced = match.OriginLeagueNameSimplified.Replace("沙巴", "", StringComparison.InvariantCultureIgnoreCase);

        outputMessage.ContentEnglish = outputMessage.ContentEnglish.Replace(match.OriginLeagueName, leagueNameReplaced, StringComparison.InvariantCultureIgnoreCase);
        var serializedContentChineseSimplified = JsonSerializer.Serialize(outputMessage.ContentChineseSimplified);
        var deSerializedContentChineseSimplified = JsonSerializer.Deserialize<string>(serializedContentChineseSimplified);
        outputMessage.ContentChineseSimplified = deSerializedContentChineseSimplified.Replace(match.OriginLeagueNameSimplified,
            leagueNameSimplifiedReplaced, StringComparison.InvariantCultureIgnoreCase);
        return outputMessage;
    }
}
