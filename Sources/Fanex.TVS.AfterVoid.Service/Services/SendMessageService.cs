using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface SendMessageService
{
    Task<Message> InitializeMessageContentForAutoVoid(Match match, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes, int issueTypeId, int? filterBy, TemplateContent playerName);

    Message InitializeMessageContentForManualVoid(VoidDataXmlModel voidData);

    Task<Message> GetMessage(int messageId);

    Task<int> InsertMessage(Message message, int matchId, int? leagueGroupId, int issueTypeId);

    Task<VoidedReason> InitializeVoidReason(bool isAutoVoid, int subIssueTypeId, int sportType, VoidDataXmlModel voidData);

    Task<int> InsertReason(string reasonEnglish, string reasonChineseSimplified);

    Task<VoidedReason> GetVoidedReason(int reasonId);

    Task SendMessage(IEnumerable<TicketEmailModel> tickets, int messageId, int issueTypeId, int creatorId);

    Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> tickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId);

    Task InsertMessageIntoAdminSite(Message message);
}

public class SendMessageServiceImp : SendMessageService
{
    private readonly IDynamicRepository repository;
    private readonly MessageItemService messageItemService;
    private readonly TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService;

    public SendMessageServiceImp(IDynamicRepository repository, MessageItemService messageItemService, TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService)
    {
        this.repository = repository;
        this.messageItemService = messageItemService;
        this.ticketVoidingSystemHttpClientService = ticketVoidingSystemHttpClientService;
    }

    public async Task SendMessage(IEnumerable<TicketEmailModel> tickets, int messageId, int issueTypeId, int creatorId)
    {
        var excludedCN88Tickets = ExcludeCN88Tickets(tickets, Helper.IsLikelyAbnormalBet(issueTypeId));
        var receivedUsers = GetListSendMessageDb(excludedCN88Tickets);
        var messageXml = messageItemService.CreateMessageItems(receivedUsers, messageId);
        if (!string.IsNullOrWhiteSpace(messageXml))
        {
            await InsertBatchMessage(creatorId, messageXml);
        }
    }

    public async Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> tickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId)
    {
        var criteria = new InsertVoidedTransactionCommand
        {
            MessageId = messageId,
            ReasonId = reasonId,
            IssueTypeId = issueTypeId,
            SubIssueTypeId = subIssueTypeId,
            TicketsXml = tickets.CreateTicketsXml()
        };

        await repository.ExecuteAsync(criteria);
    }

    public async Task<Message> InitializeMessageContentForAutoVoid(Match match, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes,
        int issueTypeId, int? filterBy, TemplateContent playerName)
    {
        var templateId = GetTemplateIdForAutoVoid(issueTypeId, filterBy);
        var templateModels = await repository.FetchAsync<TemplateModel>(new GetTemplatesCriteria()).ConfigureAwait(false);
        var template = templateModels.First(t => t.TemplateId == templateId);
        ReplaceMessageTemplate(template, match, allVoidDataInCurrentVoidTimes, issueTypeId, filterBy, playerName);

        return new Message
        {
            CreatorId = AutoVoidConstant.ActorId,
            CreatorName = AutoVoidConstant.ActorName,
            ContentEnglish = template.MsgEnglish,
            ContentChineseTraditional = template.MsgChineseTraditional,
            ContentChineseSimplified = template.MsgChineseSimplified,
            ContentNewChineseSimplified = template.MsgChineseNewSimplified
        };
    }

    public Message InitializeMessageContentForManualVoid(VoidDataXmlModel voidData)
    {
        return new Message
        {
            ContentChineseTraditional = voidData.ContentChineseTraditional,
            ContentChineseSimplified = voidData.ContentChineseSimplified,
            ContentNewChineseSimplified = voidData.ContentNewChineseSimplified,
            ContentEnglish = voidData.ContentEnglish,
            ContentSpanish = voidData.ContentSpanish,
            ContentItalian = voidData.ContentItalian,
            ContentJapanese = voidData.ContentJapanese,
            ContentKorean = voidData.ContentKorean,
            ContentThai = voidData.ContentThai,
            CreatorId = voidData.CreatorId,
            CreatorName = voidData.CreatorName
        };
    }

    public async Task<Message> GetMessage(int messageId)
    {
        var criteria = new GetMessageCriteria
        {
            MessageId = messageId
        };

        return await repository.GetAsync<Message>(criteria);
    }

    public async Task<int> InsertMessage(Message message, int matchId, int? leagueGroupId, int issueTypeId)
    {
        var messageContentType = Helper.GetMessageContentType(issueTypeId, message.ContentEnglish);
        var criteria = new CreateMessageCriteria
        {
            MatchId = matchId,
            LeagueGroupId = leagueGroupId,
            Creator = message.CreatorId,
            CreatorName = message.CreatorName,
            Content_en = message.ContentEnglish.InsertSpace(),
            Content_ch = message.ContentChineseTraditional.InsertSpace(),
            Content_cs = message.ContentChineseSimplified.InsertSpace(),
            Content_zhcn = message.ContentNewChineseSimplified.InsertSpace(),
            Content_it = message.ContentItalian.InsertSpace(),
            Content_jp = message.ContentJapanese.InsertSpace(),
            Content_th = message.ContentThai.InsertSpace(),
            Content_ko = message.ContentKorean.InsertSpace(),
            Content_es = message.ContentSpanish.InsertSpace(),
            MsgContentsType = messageContentType
        };

        return await repository.GetAsync<int>(criteria);
    }

    public async Task<VoidedReason> InitializeVoidReason(bool isAutoVoid, int subIssueTypeId, int sportType, VoidDataXmlModel voidData)
    {
        if (isAutoVoid)
        {
            var criteria = new GetReasonNameCriteria
            {
                SportType = sportType,
                SubIssueTypeId = subIssueTypeId
            };
            var reason = await repository.GetAsync<VoidedReason>(criteria);
            if (reason == null)
            {
                return new VoidedReason();
            }
            var sportEn = "";
            var sportCS = "";
            if (sportType > 0)
            {
                sportEn = $"{await ticketVoidingSystemHttpClientService.BetListGetSportTypeName(sportType, LanguageCode.English)} - ";
                sportCS = $"{await ticketVoidingSystemHttpClientService.BetListGetSportTypeName(sportType, LanguageCode.ChineseSimplified)} - ";
            }
            reason.ReasonEnglish = $"{sportEn}{reason.ReasonEnglish}";
            reason.ReasonChineseSimplified = $"{sportCS}{reason.ReasonChineseSimplified}";

            return reason;
        }

        return new VoidedReason
        {
            ReasonEnglish = voidData.Reason,
            ReasonChineseSimplified = voidData.ReasonSC
        };
    }

    public async Task<int> InsertReason(string reasonEnglish, string reasonChineseSimplified)
    {
        var criteria = new InsertReasonCommand
        {
            ReasonEnglish = reasonEnglish,
            ReasonChineseSimplified = reasonChineseSimplified
        };

        await repository.ExecuteAsync(criteria);
        return criteria.ReasonId;
    }

    public async Task<VoidedReason> GetVoidedReason(int reasonId)
    {
        var criteria = new GetVoidedReasonCriteria
        {
            ReasonId = reasonId
        };

        return await repository.GetAsync<VoidedReason>(criteria);
    }

    public async Task InsertBatchMessage(int userId, string message)
    {
        var command = new SendMessageCommand
        {
            CreatorId = userId,
            XmlMessage = message
        };
        await repository.ExecuteAsync(command);
    }

    public async Task InsertMessageIntoAdminSite(Message message)
    {
        var command = new InsertMessageIntoAdminSiteCriteria
        {
            men = message.ContentEnglish,
            mch = message.ContentChineseTraditional,
            member = true,
            agent = true,
            ma = true,
            sma = true,
            creator = message.CreatorId
        };
        await repository.ExecuteAsync(command);
    }

    private static IEnumerable<TicketEmailModel> ExcludeCN88Tickets(IEnumerable<TicketEmailModel> tickets, bool isAbnormalBet)
    {
        return isAbnormalBet ? tickets.Where(x => !x.Site.ToLowerInvariant().Equals("cn88")) : tickets;
    }

    public static IEnumerable<ReceivedUser> GetListSendMessageDb<T>(IEnumerable<T> tickets) where T : SendMessageDbModel
    {
        return tickets.Select(item => new ReceivedUser
        {
            CustId = item.CustId,
            CustUserName = item.UserName,
            Recommend = item.Recommend,
            AgUserName = item.AgUserName,
            Mrecommend = item.Mrecommend,
            MagUserName = item.MagUserName,
            Srecommend = item.Srecommend,
            SagUserName = item.SagUserName,
            TransId = item.TransId
        });
    }

    private static void ReplaceMessageTemplate(TemplateModel model, Match match, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes,
        int issueTypeId, int? filterBy, TemplateContent playerName)
    {
        ReplaceSportType(model, match);
        ReplaceHomeTeam(model, match);
        ReplaceAwayTeam(model, match);
        ReplaceLeagueName(model, match);
        ReplaceEventDate(model, match);
        ReplaceCheckStatement(model, allVoidDataInCurrentVoidTimes);
        ReplaceParlay(model, issueTypeId, filterBy);
        ReplaceEventType(model);
        ReplacePlacedFromTimeRange(model, allVoidDataInCurrentVoidTimes);
        ReplacePlayerName(model, playerName);
    }

    private static void ReplacePlayerName(TemplateModel model, TemplateContent playerName)
    {
        const string playerNamePlaceholder = "{{Player Name}}";
        model.MsgEnglish = model.MsgEnglish.Replace(playerNamePlaceholder, playerName?.MsgEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(playerNamePlaceholder, playerName?.MsgChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(playerNamePlaceholder, playerName?.MsgChineseNewSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(playerNamePlaceholder, playerName?.MsgChineseNewSimplified);
    }

    private static void ReplaceSportType(TemplateModel model, Match match)
    {
        const string sportTypePlaceholder = "{{Sport type}}";
        model.MsgEnglish = model.MsgEnglish.Replace(sportTypePlaceholder, match?.SportTypeName);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(sportTypePlaceholder, match?.SportTypeNameTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(sportTypePlaceholder, match?.SportTypeNameSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(sportTypePlaceholder, match?.SportTypeNameNewSimplified);
    }

    private static void ReplaceHomeTeam(TemplateModel model, Match match)
    {
        const string homeTeamPlaceholder = "{{Home team name}}";
        model.MsgEnglish = model.MsgEnglish.Replace(homeTeamPlaceholder, match?.HomeName);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(homeTeamPlaceholder, match?.HomeNameTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(homeTeamPlaceholder, match?.HomeNameSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(homeTeamPlaceholder, match?.HomeNameNewSimplified);
    }

    private static void ReplaceAwayTeam(TemplateModel model, Match match)
    {
        const string awayTeamPlaceholder = "{{Away team name}}";
        model.MsgEnglish = model.MsgEnglish.Replace(awayTeamPlaceholder, match?.AwayName);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(awayTeamPlaceholder, match?.AwayNameTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(awayTeamPlaceholder, match?.AwayNameSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(awayTeamPlaceholder, match?.AwayNameNewSimplified);
    }

    private static void ReplaceLeagueName(TemplateModel model, Match match)
    {
        const string leagueNamePlaceholder = "{{League name}}";
        model.MsgEnglish = model.MsgEnglish.Replace(leagueNamePlaceholder, match?.LeagueName);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(leagueNamePlaceholder, match?.LeagueNameTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(leagueNamePlaceholder, match?.LeagueNameSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(leagueNamePlaceholder, match?.LeagueNameNewSimplified);
    }

    private static void ReplaceEventDate(TemplateModel model, Match match)
    {
        const string eventDatePlaceholder = "{{Event Date}}";
        const string eventDateFormat = "dd/MM";
        model.MsgEnglish = model.MsgEnglish.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(eventDateFormat));
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(eventDateFormat));
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(eventDateFormat));
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(eventDateFormat));
    }

    private static void ReplaceCheckStatement(TemplateModel model, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes)
    {
        var isHasSettledTicket = allVoidDataInCurrentVoidTimes.Any(x => x.IsHasSettledTicket);
        const string checkStatementPlaceholder = "{{CheckStatement}}";
        var checkStatementEnglish = isHasSettledTicket ? "Kindly double check your statement again. " : "";
        var checkStatementChineseTraditional = isHasSettledTicket ? "請重新檢查您的帳目. " : "";
        var checkStatementChineseSimplified = isHasSettledTicket ? "请重新检查您的帐目. " : "";
        var checkStatementChineseNewSimplified = isHasSettledTicket ? "请重新检查您的帐目. " : "";
        model.MsgEnglish = model.MsgEnglish.Replace(checkStatementPlaceholder, checkStatementEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(checkStatementPlaceholder, checkStatementChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(checkStatementPlaceholder, checkStatementChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(checkStatementPlaceholder, checkStatementChineseNewSimplified);
    }

    private static void ReplaceParlay(TemplateModel model, int issueTypeId, int? filterBy)
    {
        const string parlayPlaceholder = "{{Parlay}}";
        var parlayEnglish = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[Parlay]" : "";
        var parlayChineseTraditional = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串關]" : "";
        var parlayChineseSimplified = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串关]" : "";
        var parlayChineseNewSimplified = Helper.IsAbnormalBetMixParlay(issueTypeId, filterBy) ? "[串关]" : "";
        model.MsgEnglish = model.MsgEnglish.Replace(parlayPlaceholder, parlayEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(parlayPlaceholder, parlayChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(parlayPlaceholder, parlayChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(parlayPlaceholder, parlayChineseNewSimplified);
    }

    private static void ReplaceEventType(TemplateModel model)
    {
        ReplaceForEachLanguage(model, TemplatePlaceholderConstant.EventType, TemplateContentConstant.Empty);
    }

    private static void ReplacePlacedFromTimeRange(TemplateModel model, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes)
    {
        const int numberFive = 5;
        var totalTicket = allVoidDataInCurrentVoidTimes.Sum(x => x.TotalTicket);

        if (totalTicket <= numberFive)
        {
            ReplaceForEachLanguage(model, TemplatePlaceholderConstant.PlacedFromTimeRange, TemplateContentConstant.Empty);
        }
        else
        {
            var timeRange = GetTimeRange(allVoidDataInCurrentVoidTimes);
            var newContent = new TemplateContent
            {
                MsgEnglish = $"{TemplateContentConstant.PlacedFrom.MsgEnglish} {timeRange}{TemplateContentConstant.Gmt8.MsgEnglish}",
                MsgChineseTraditional = $"{TemplateContentConstant.PlacedFrom.MsgChineseTraditional} {timeRange}{TemplateContentConstant.Gmt8.MsgChineseTraditional}",
                MsgChineseSimplified = $"{TemplateContentConstant.PlacedFrom.MsgChineseSimplified} {timeRange}{TemplateContentConstant.Gmt8.MsgChineseSimplified}",
                MsgChineseNewSimplified = $"{TemplateContentConstant.PlacedFrom.MsgChineseNewSimplified} {timeRange}{TemplateContentConstant.Gmt8.MsgChineseNewSimplified}"
            };
            ReplaceForEachLanguage(model, TemplatePlaceholderConstant.PlacedFromTimeRange, newContent);
        }
    }

    private static string GetTimeRange(IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes)
    {
        var voidFromTime = allVoidDataInCurrentVoidTimes.Select(x => x.VoidFromTime).Select(DateTime.Parse).Min(x => x)
            .ToString(FormatKey.LongTimeFormat);
        var voidToTime = allVoidDataInCurrentVoidTimes.Select(x => x.VoidToTime).Select(DateTime.Parse).Max(x => x)
            .ToString(FormatKey.LongTimeFormat);

        return $"{voidFromTime} - {voidToTime}";
    }

    private static void ReplaceForEachLanguage(TemplateModel model, string placeholder, TemplateContent newContent)
    {
        model.MsgEnglish = model.MsgEnglish.Replace(placeholder, newContent.MsgEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(placeholder, newContent.MsgChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(placeholder, newContent.MsgChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(placeholder, newContent.MsgChineseNewSimplified);
    }

    private static int GetTemplateIdForAutoVoid(int issueTypeId, int? filterBy)
    {
        const int filterMixParlay = 1;

        switch (issueTypeId)
        {
            case IssueTypeConstant.AbnormalBet when filterBy is null:
            case IssueTypeConstant.AbnormalBetRealMember:
                return MessageTemplateIdConstant.ThirtyFour;

            case IssueTypeConstant.AbnormalBet when filterBy is filterMixParlay:
                return MessageTemplateIdConstant.ThirtySeven;

            case IssueTypeConstant.DuplicateTickets when filterBy is null:
                return MessageTemplateIdConstant.SixtyFour;

            case IssueTypeConstant.DuplicateTickets when filterBy is filterMixParlay:
                return MessageTemplateIdConstant.SixtyFive;

            case IssueTypeConstant.AbnormalAccount:
                return MessageTemplateIdConstant.ThirtySeven;

            case IssueTypeConstant.ReducedOver:
                return MessageTemplateIdConstant.ThirtyEight;

            case IssueTypeConstant.NotParticipate:
                return MessageTemplateIdConstant.SixtyThree;

            default:
                return MessageTemplateIdConstant.ThirtySeven;
        }
    }
}