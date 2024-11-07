using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fanex.TVS.AfterVoid.Service.Models;
using Force.DeepCloner;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Extensions;
using TicketVoidingSystem.Models;
using MatchModel = TicketVoidingSystem.Models.Match;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface SpecialPlatformService
{
    (IEnumerable<TicketEmailModel> NormalGroup, IEnumerable<TicketEmailModel> SpecialPlatformGroup) GroupTicketSaba(IEnumerable<TicketEmailModel> tickets);

    Message RemoveSabaInMessageContent(MatchModel match, Message message, RequestDetailViewModel requestInfo);
}

public class SpecialPlatformServiceImp : SpecialPlatformService
{
    private const string BrandNameSaba = "SABA ";

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

    public Message RemoveSabaInMessageContent(MatchModel match, Message message, RequestDetailViewModel requestInfo)
    {
        var outputMessage = message.DeepClone();

        RemoveSabaLeagueNameByMatch(match, outputMessage);
        RemoveSabaLeagueNameByInput(requestInfo, outputMessage);

        return outputMessage;
    }

    public void RemoveSabaLeagueNameByMatch(MatchModel match, Message outputMessage)
    {
        if (match == null)
        {
            return;
        }

        outputMessage.ContentEnglish = ProcessLeagueNameReplacement(match.OriginLeagueName, outputMessage.ContentEnglish);
        outputMessage.ContentChineseSimplified = ProcessLeagueNameReplacement(match.OriginLeagueNameSimplified, outputMessage.ContentChineseSimplified);
        outputMessage.ContentChineseTraditional = ProcessLeagueNameReplacement(match.OriginLeagueNameTraditional, outputMessage.ContentChineseTraditional);
        outputMessage.ContentNewChineseSimplified = ProcessLeagueNameReplacement(match.OriginLeagueNameNewSimplified, outputMessage.ContentNewChineseSimplified);
    }

    public void RemoveSabaLeagueNameByInput(RequestDetailViewModel requestInfo, Message outputMessage)
    {
        const int leagueNameEnglish = 128;
        const int leagueNameChinese = 129;
        var subIssueTypeId = requestInfo.EventByMatch.SubIssueTypeId;

        if (requestInfo.Request.IssueTypeId is IssueTypeConstant.WrongOfferedName
            && subIssueTypeId is leagueNameEnglish or leagueNameChinese)
        {
            RemoveSabaWrongName(requestInfo, outputMessage);
            RemoveSabaActualName(requestInfo, outputMessage);
        }
    }

    public void RemoveSabaWrongName(RequestDetailViewModel requestInfo, Message outputMessage)
    {
        outputMessage.ContentEnglish = ProcessLeagueNameReplacement(requestInfo.EventByMatch.WrongName, outputMessage.ContentEnglish);
        outputMessage.ContentChineseSimplified = ProcessLeagueNameReplacement(requestInfo.EventByMatch.WrongNameS, outputMessage.ContentChineseSimplified);
        outputMessage.ContentChineseTraditional = ProcessLeagueNameReplacement(requestInfo.EventByMatch.WrongNameT, outputMessage.ContentChineseTraditional);
        outputMessage.ContentNewChineseSimplified = ProcessLeagueNameReplacement(requestInfo.EventByMatch.WrongNameS, outputMessage.ContentNewChineseSimplified);
    }

    public void RemoveSabaActualName(RequestDetailViewModel requestInfo, Message outputMessage)
    {
        outputMessage.ContentEnglish = ProcessLeagueNameReplacement(requestInfo.EventByMatch.ActualName, outputMessage.ContentEnglish);
        outputMessage.ContentChineseSimplified = ProcessLeagueNameReplacement(requestInfo.EventByMatch.ActualNameS, outputMessage.ContentChineseSimplified);
        outputMessage.ContentChineseTraditional = ProcessLeagueNameReplacement(requestInfo.EventByMatch.ActualNameT, outputMessage.ContentChineseTraditional);
        outputMessage.ContentNewChineseSimplified = ProcessLeagueNameReplacement(requestInfo.EventByMatch.ActualNameS, outputMessage.ContentNewChineseSimplified);
    }

    public string ReplaceEachChineseLanguage(string source, string oldValue, string newValue)
    {
        var replaced = source.Replace(oldValue, "", StringComparison.InvariantCultureIgnoreCase)
            .Replace(BrandNameSaba, "", StringComparison.InvariantCultureIgnoreCase);
        var serializedSource = JsonSerializer.Serialize(source);
        var deSerializedSource = JsonSerializer.Deserialize<string>(serializedSource);

        return deSerializedSource.Replace(source, replaced, StringComparison.InvariantCultureIgnoreCase);
    }

    private static string ProcessLeagueNameReplacement(string originalLeagueName, string content)
    {
        if (string.IsNullOrWhiteSpace(originalLeagueName))
        {
            return content;
        }

        var leagueNameReplaced = originalLeagueName
            .Replace("沙巴", "", StringComparison.InvariantCultureIgnoreCase)
            .Replace(BrandNameSaba, "", StringComparison.InvariantCultureIgnoreCase);

        var serializedContent = JsonSerializer.Serialize(content);
        var deserializedContent = JsonSerializer.Deserialize<string>(serializedContent);

        return deserializedContent.Replace(originalLeagueName, leagueNameReplaced, StringComparison.InvariantCultureIgnoreCase);
    }
}