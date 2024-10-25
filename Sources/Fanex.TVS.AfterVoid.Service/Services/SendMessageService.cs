using System;
using System.Collections.Generic;
using System.Globalization;
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
    Task<Message> InitializeMessageContentForAutoVoid(Match match, VoidDataXmlModel voidDataXml, RequestDetailViewModel requestInfo);

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

public class SendMessageServiceImp(
    IDynamicRepository repository,
    MessageItemService messageItemService,
    RemotingRefWrapperService remotingRefWrapperService)
    : SendMessageService
{
    public async Task SendMessage(IEnumerable<TicketEmailModel> tickets, int messageId, int issueTypeId, int creatorId)
    {
        var excludedCN88Tickets = ExcludeCN88Tickets(tickets, Helper.IsAbnormalBet(issueTypeId));
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

    public async Task<Message> InitializeMessageContentForAutoVoid(Match match, VoidDataXmlModel voidDataXml, RequestDetailViewModel requestInfo)
    {
        var templateModels = await repository.FetchAsync<TemplateModel>(new GetTemplatesCriteria()).ConfigureAwait(false);
        var templateId = await GetTemplateIdForAutoVoid(requestInfo);
        var template = templateModels.First(t => t.TemplateId == templateId);
        ReplaceMessageTemplate(template, match, voidDataXml, requestInfo);

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
            return await InitializeVoidReasonForAuto(subIssueTypeId, sportType);
        }

        return new VoidedReason
        {
            ReasonEnglish = voidData.Reason,
            ReasonChineseSimplified = voidData.ReasonSC
        };
    }

    private async Task<VoidedReason> InitializeVoidReasonForAuto(int subIssueTypeId, int sportType)
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

        var sportEn = sportType > 0 ? $"{remotingRefWrapperService.GetSportName(sportType.ToString(), LanguageCode.English)} - " : string.Empty;
        var sportCS = sportType > 0 ? $"{remotingRefWrapperService.GetSportName(sportType.ToString(), LanguageCode.ChineseSimplified)} - " : string.Empty;
        reason.ReasonEnglish = $"{sportEn}{reason.ReasonEnglish}";
        reason.ReasonChineseSimplified = $"{sportCS}{reason.ReasonChineseSimplified}";

        return reason;
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

    private static void ReplaceMessageTemplate(TemplateModel model, Match match, VoidDataXmlModel voidDataXml, RequestDetailViewModel requestInfo)
    {
        ReplaceSportType(model, match);
        ReplaceHomeTeam(model, match);
        ReplaceAwayTeam(model, match);
        ReplaceLeagueName(model, match);
        ReplaceEventDate(model, match);
        ReplaceKoTime(model, requestInfo.EventByMatch);
        ReplaceCheckStatement(model, voidDataXml);
        ReplaceParlay(model, requestInfo.Request.IssueTypeId, requestInfo.EventByMatch.FilterBy);
        ReplaceEventType(model, requestInfo.Request.IssueTypeId, requestInfo.IsAllLiveTickets);
        ReplacePlacedFromTimeRange(model, voidDataXml, requestInfo.IsAllLiveTickets);
        ReplacePlayerName(model, requestInfo.PlayerNamesSelected);
        ReplaceSpecialBetType(model, requestInfo.SpecialBetTypeContent);
        ReplaceWrongName(model, requestInfo.EventByMatch);
    }

    private static void ReplacePlayerName(TemplateModel model, ContentByLanguageModel playerName)
    {
        const string playerNamePlaceholder = "{{Player Name}}";
        model.MsgEnglish = model.MsgEnglish.Replace(playerNamePlaceholder, playerName?.ContentEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(playerNamePlaceholder, playerName?.ContentChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(playerNamePlaceholder, playerName?.ContentChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(playerNamePlaceholder, playerName?.ContentNewChineseSimplified);
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

        model.MsgEnglish = model.MsgEnglish.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(FormatKey.TemplateMessageDateFormat));
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(FormatKey.TemplateMessageDateFormat));
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(FormatKey.TemplateMessageDateFormat));
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(eventDatePlaceholder, match?.KickOffTime.ToString(FormatKey.TemplateMessageDateFormat));
    }

    private static void ReplaceKoTime(TemplateModel model, RequestEventModel eventByMatch)
    {
        var kickOffTimeDateGmt8 = eventByMatch.KickOffTime?.AddHours(12).AddSeconds(-1);
        const string eventDatePlaceholder = "{{KO time}}";

        model.MsgEnglish = model.MsgEnglish.Replace(eventDatePlaceholder, kickOffTimeDateGmt8?.ToString(FormatKey.TemplateMessageDateTimeFormat));
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(eventDatePlaceholder, kickOffTimeDateGmt8?.ToString(FormatKey.TemplateMessageDateTimeFormat));
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(eventDatePlaceholder, kickOffTimeDateGmt8?.ToString(FormatKey.TemplateMessageDateTimeFormat));
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(eventDatePlaceholder, kickOffTimeDateGmt8?.ToString(FormatKey.TemplateMessageDateTimeFormat));
    }

    private static void ReplaceCheckStatement(TemplateModel model, VoidDataXmlModel voidDataXml)
    {
        var isHasSettledTicket = voidDataXml.IsHasSettledTicket;
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
        var parlayEnglish = Helper.IsAbnormalBetParlay(issueTypeId, filterBy) ? "[Parlay]" : "";
        var parlayChineseTraditional = Helper.IsAbnormalBetParlay(issueTypeId, filterBy) ? "[串關]" : "";
        var parlayChineseSimplified = Helper.IsAbnormalBetParlay(issueTypeId, filterBy) ? "[串关]" : "";
        var parlayChineseNewSimplified = Helper.IsAbnormalBetParlay(issueTypeId, filterBy) ? "[串关]" : "";
        model.MsgEnglish = model.MsgEnglish.Replace(parlayPlaceholder, parlayEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(parlayPlaceholder, parlayChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(parlayPlaceholder, parlayChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(parlayPlaceholder, parlayChineseNewSimplified);
    }

    private static void ReplaceEventType(TemplateModel model, int issueTypeId, bool isAllLiveTickets)
    {
        var issueTypesNeedCheckAllLiveTickets = new List<int>
        {
            IssueTypeConstant.WrongHDP, IssueTypeConstant.Wrong1stHDP,
            IssueTypeConstant.WrongOU, IssueTypeConstant.Wrong1stOU,
            IssueTypeConstant.WrongOdds, IssueTypeConstant.ReducedOver, IssueTypeConstant.Abandoned,
            IssueTypeConstant.NotParticipate, IssueTypeConstant.WrongMarketOffered, IssueTypeConstant.TechnicalError
        };

        var eventType = issueTypeId switch
        {
            IssueTypeConstant.DangerAccepted or IssueTypeConstant.RedCard or IssueTypeConstant.WrongScore => TemplateContentConstant.Live,
            _ when issueTypesNeedCheckAllLiveTickets.Contains(issueTypeId) => isAllLiveTickets ? TemplateContentConstant.Live : TemplateContentConstant.Empty,
            _ => TemplateContentConstant.Empty
        };

        ReplaceForEachLanguage(model, TemplatePlaceholderConstant.EventType, eventType);
    }

    private static void ReplacePlacedFromTimeRange(TemplateModel model, VoidDataXmlModel voidDataXml, bool isAllLiveTickets)
    {
        const int fiveTickets = 5;
        if (voidDataXml.TotalTicket > fiveTickets)
        {
            var timeRange = GetTimeRange(voidDataXml);
            var newContent = new ContentByLanguageModel
            {
                ContentEnglish = $"{TemplateContentConstant.PlacedFrom.ContentEnglish} {timeRange} {TemplateContentConstant.Gmt8.ContentEnglish} ",
                ContentChineseTraditional = $"{TemplateContentConstant.PlacedFrom.ContentChineseTraditional} {timeRange} {TemplateContentConstant.Gmt8.ContentChineseTraditional} ",
                ContentChineseSimplified = $"{TemplateContentConstant.PlacedFrom.ContentChineseSimplified} {timeRange} {TemplateContentConstant.Gmt8.ContentChineseSimplified} ",
                ContentNewChineseSimplified = $"{TemplateContentConstant.PlacedFrom.ContentNewChineseSimplified} {timeRange} {TemplateContentConstant.Gmt8.ContentNewChineseSimplified} "
            };
            ReplaceForEachLanguage(model, TemplatePlaceholderConstant.PlacedFromTimeRange, newContent);
        }
        else
        {
            var newContent = voidDataXml.TotalTicket <= fiveTickets && isAllLiveTickets
                ? TemplateContentConstant.PlacedAt
                : TemplateContentConstant.Empty;
            ReplaceForEachLanguage(model, TemplatePlaceholderConstant.PlacedFromTimeRange, newContent);
        }
    }

    private static string GetTimeRange(VoidDataXmlModel voidDataXml)
    {
        var voidFromDateTime = DateTime.Parse(voidDataXml.VoidFromTime, CultureInfo.InvariantCulture);
        var voidToDateTime = DateTime.Parse(voidDataXml.VoidToTime, CultureInfo.InvariantCulture);
        var isTheSameDay = voidFromDateTime.Date == voidToDateTime.Date;
        var displayFromTime = voidFromDateTime.ToString(FormatKey.TemplateMessageDateTimeFormat);
        var displayToTime = isTheSameDay ? voidToDateTime.ToString(FormatKey.TemplateMessageTimeWithAmPmFormat) : voidToDateTime.ToString(FormatKey.TemplateMessageDateTimeFormat);

        return $"{displayFromTime} - {displayToTime}";
    }

    private static void ReplaceForEachLanguage(TemplateModel model, string placeholder, ContentByLanguageModel newContent)
    {
        model.MsgEnglish = model.MsgEnglish.Replace(placeholder, newContent.ContentEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(placeholder, newContent.ContentChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(placeholder, newContent.ContentChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(placeholder, newContent.ContentNewChineseSimplified);
    }

    private async Task<int> GetTemplateIdForAutoVoid(RequestDetailViewModel requestInfo)
    {
        var issueTypeId = requestInfo.Request.IssueTypeId;
        var sportType = requestInfo.SportType;
        var filterBy = requestInfo.EventByMatch.FilterBy;
        var subIssueTypeId = requestInfo.EventByMatch.SubIssueTypeId;
        var templateSettings = await repository.FetchAsync<TemplateSetting>(new GetTemplateSettingCriteria()).ConfigureAwait(false);

        return issueTypeId switch
        {
            IssueTypeConstant.DuplicateTickets when filterBy == FilterByConstant.MixParlay => MessageTemplateIdConstant.SixtyFive,
            IssueTypeConstant.AbnormalBet when Helper.IsAbnormalBetParlay(issueTypeId, filterBy) => MessageTemplateIdConstant.ThirtySeven,
            _ => GetTemplateIdByFilter(subIssueTypeId, sportType, templateSettings)
        };
    }

    private static int GetTemplateIdByFilter(int subIssueType, int? sportType, IEnumerable<TemplateSetting> templateSetting)
    {
        var settingsBySubType = templateSetting.Where(x => x.SubIssueTypeId == subIssueType);
        var settingBySportType = settingsBySubType.FirstOrDefault(x => x.SportType == sportType);
        var defaultTemplateWhenSportNull = settingsBySubType.FirstOrDefault(x => x.SportType == null);

        return settingBySportType?.TemplateId ?? defaultTemplateWhenSportNull?.TemplateId ?? MessageTemplateIdConstant.ThirtySeven;
    }

    private static void ReplaceSpecialBetType(TemplateModel model, ContentByLanguageModel specialBetTypeContent)
    {
        var betTypeEnglish = string.IsNullOrWhiteSpace(specialBetTypeContent.ContentEnglish) ? string.Empty : $"\"{specialBetTypeContent.ContentEnglish}\" ";
        var betTypeChineseTraditional = string.IsNullOrWhiteSpace(specialBetTypeContent.ContentChineseTraditional) ? string.Empty : $"\"{specialBetTypeContent.ContentChineseTraditional}\" ";
        var betTypeChineseSimplified = string.IsNullOrWhiteSpace(specialBetTypeContent.ContentChineseSimplified) ? string.Empty : $"\"{specialBetTypeContent.ContentChineseSimplified}\" ";
        var betTypeNewChineseSimplified = string.IsNullOrWhiteSpace(specialBetTypeContent.ContentNewChineseSimplified) ? string.Empty : $"\"{specialBetTypeContent.ContentNewChineseSimplified}\" ";

        const string specialBetTypePlaceholder = "{{SpecialBetType}}";
        model.MsgEnglish = model.MsgEnglish.Replace(specialBetTypePlaceholder, betTypeEnglish);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(specialBetTypePlaceholder, betTypeChineseTraditional);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(specialBetTypePlaceholder, betTypeChineseSimplified);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(specialBetTypePlaceholder, betTypeNewChineseSimplified);
    }

    private static void ReplaceWrongName(TemplateModel model, RequestEventModel eventModel)
    {
        const string wrongNamePlaceholder = "{{Wrong Name}}";
        const string actualNamePlaceholder = "{{Actual Name}}";
        const string wrongNameTPlaceholder = "{{Wrong NameT}}";
        const string actualNameTPlaceholder = "{{Actual NameT}}";
        const string wrongNameSPlaceholder = "{{Wrong NameS}}";
        const string actualNameSPlaceholder = "{{Actual NameS}}";

        ReplaceAllLanguage(model, wrongNamePlaceholder, eventModel.WrongName);
        ReplaceAllLanguage(model, actualNamePlaceholder, eventModel.ActualName);
        ReplaceAllLanguage(model, wrongNameTPlaceholder, eventModel.WrongNameT);
        ReplaceAllLanguage(model, actualNameTPlaceholder, eventModel.ActualNameT);
        ReplaceAllLanguage(model, wrongNameSPlaceholder, eventModel.WrongNameS);
        ReplaceAllLanguage(model, actualNameSPlaceholder, eventModel.ActualNameS);
    }

    private static void ReplaceAllLanguage(TemplateModel model, string destination, string value)
    {
        model.MsgEnglish = model.MsgEnglish.Replace(destination, value);
        model.MsgChineseTraditional = model.MsgChineseTraditional.Replace(destination, value);
        model.MsgChineseSimplified = model.MsgChineseSimplified.Replace(destination, value);
        model.MsgChineseNewSimplified = model.MsgChineseNewSimplified.Replace(destination, value);
    }
}