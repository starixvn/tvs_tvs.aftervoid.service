using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface SendLetstalkServiceStage
{
    Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk();

    Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds);

    Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList);

    Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds);

    Task WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime);

    Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId);

    NotificationLetstalkModel CreateNotificationModel(
        GroupVoidBetAutomationInfo group,
        Match matchInfo,
        IEnumerable<VoidDataXmlModel> voidDataXmlList,
        IEnumerable<TicketEmailModel> emailTickets);

    bool IsTechnicalErrorProviderMistake(RequestDetailViewModel requestInfo);
}

public class SendLetstalkServiceStageImp(
    IDynamicRepository repository,
    LoggingService loggingService,
    VoidBetAutomationService voidBetAutomationService,
    TVSWebSiteHttpClientService tvsWebSiteHttpClientService)
    : SendLetstalkServiceStage
{
    public async Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk()
    {
        var criteria = new GetAutomationInfoLetstalkCriteria
        {
            OnlineServerId = string.Join(",", SiteDbGroupConstant.ServerList.Select(x => x.Id))
        };

        return await repository.FetchAsync<VoidBetAutomationInfo>(criteria);
    }

    public async Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds)
    {
        var (isSentEmail, results) = await tvsWebSiteHttpClientService.GetExcelVoidedTicketForSendLetstalk(requestId, matchId, issueTypeId, voidingHistoryIds);
        if (isSentEmail)
        {
            return results;
        }

        throw new ExternalException(nameof(GetContentExcelVoidedTicket));
    }

    public async Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList)
    {
        var voidedTickets = new List<TicketEmailModel>();
        foreach (var item in voidBetAutomationList)
        {
            var ticketsInServer =
                await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(item.VoidingHistoryId, item.SiteDbGroupId, false);
            voidedTickets.AddRange(ticketsInServer);
        }

        return voidedTickets;
    }

    public async Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds)
    {
        var allVoidDataInCurrentVoidTimes = await voidBetAutomationService.GetAllVoidDataFromMultiVoidingHistory(voidingHistoryIds);
        return allVoidDataInCurrentVoidTimes.Where(x => voidingHistoryIds.Contains(x.VoidingHistoryId));
    }

    public async Task WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime)
    {
        var logBuilder = new StringBuilder();
        logBuilder.Append($"SendLetstalk - RequestId: {group.RequestId} - " +
                          $"BroadcastSessionId: {group.BroadcastSessionId} - Execute Time: {startTime.ElapsedMilliseconds} ms \n");
        foreach (var result in results)
        {
            var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
            logBuilder.Append(log);
        }

        await loggingService.InfoAsync(logBuilder.ToString());
    }

    public async Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId)
    {
        var command = new UpdateLetstalkCommand
        {
            RequestId = requestId,
            BroadcastSessionId = broadcastSessionId,
            StatusId = statusId
        };

        await repository.ExecuteAsync(command);
    }

    public NotificationLetstalkModel CreateNotificationModel(
        GroupVoidBetAutomationInfo group,
        Match matchInfo,
        IEnumerable<VoidDataXmlModel> voidDataXmlList,
        IEnumerable<TicketEmailModel> emailTickets)
    {
        var model = new NotificationLetstalkModel
        {
            Match = matchInfo,
            UserName = group.RequestInfo.Request.VoidedBy,
            DecidedBy = group.RequestInfo.Request.DecidedBy,
            IsMemberTemplate = !(matchInfo?.MatchId > 0),
            IssueTypeId = group.RequestInfo.Request.IssueTypeId,
            FilterBy = group.RequestInfo.EventByMatch.FilterBy,
            RequestId = group.RequestId,
            RequestedBy = group.RequestInfo.Request.RequestedBy,
            VoidingHistoryIds = string.Join(",", group.VoidBetAutomationList.Select(x => x.VoidingHistoryId))
        };

        if (IsAbnormalAccountVoidInstant(group))
        {
            UpdateTimeByTickets(model, emailTickets);
        }
        else
        {
            UpdateTimeByVoidDataXml(model, voidDataXmlList);
        }

        return model;
    }

    private static void UpdateTimeByVoidDataXml(NotificationLetstalkModel letstalkModel, IEnumerable<VoidDataXmlModel> voidDataXmlList)
    {
        letstalkModel.VoidFromTime = voidDataXmlList.Any()
            ? voidDataXmlList.Select(x => x.VoidFromTime).Select(DateTime.Parse).Min(x => x)
                .ToString(FormatKey.TimeWithAmPm)
            : null;
        letstalkModel.VoidToTime = voidDataXmlList.Any()
            ? voidDataXmlList.Select(x => x.VoidToTime).Select(DateTime.Parse).Max(x => x)
                .ToString(FormatKey.TimeWithAmPm)
            : null;
        letstalkModel.VoidFromDate = voidDataXmlList.Any()
            ? voidDataXmlList.Select(x => x.VoidFromDate).Select(DateTime.Parse).Min(x => x)
                .ToString(FormatKey.ShortDateFormatWithSlash)
            : null;
        letstalkModel.VoidToDate = voidDataXmlList.Any()
            ? voidDataXmlList.Select(x => x.VoidToDate).Select(DateTime.Parse).Min(x => x)
                .ToString(FormatKey.ShortDateFormatWithSlash)
            : null;
    }

    private static void UpdateTimeByTickets(NotificationLetstalkModel letstalkModel, IEnumerable<TicketEmailModel> emailTickets)
    {
        letstalkModel.VoidFromDate = emailTickets.Select(x => x?.WinLostDate).Min(x => x)?.ZoneManila()
            .ToString(FormatKey.ShortDateFormatWithSlash);
        letstalkModel.VoidToDate = emailTickets.Select(x => x.WinLostDate).Max(x => x)?.ZoneManila()
            .ToString(FormatKey.ShortDateFormatWithSlash);
    }

    private static bool IsAbnormalAccountVoidInstant(GroupVoidBetAutomationInfo group)
    {
        return group.IsAutoVoid && group.RequestInfo.Request.IssueTypeId is IssueTypeConstant.AbnormalAccount;
    }

    public bool IsTechnicalErrorProviderMistake(RequestDetailViewModel requestInfo)
    {
        if (requestInfo.Request.IssueTypeId == IssueTypeConstant.TechnicalError)
        {
            var subIssueTypeId = requestInfo.EventByMatch.SubIssueTypeId;
            if (subIssueTypeId == SubIssueTypeConstant.TechnicalError_ProviderMistake)
            {
                return true;
            }
        }

        return false;
    }
}