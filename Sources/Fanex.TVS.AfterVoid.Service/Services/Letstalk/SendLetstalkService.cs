using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface SendLetstalkService
{
    Task Execute();
}

public class SendLetstalkServiceImp(
    VoidBetAutomationService voidBetAutomationService,
    AbnormalBetLetstalkService abnormalBetLetstalkServiceImp,
    SettledBetLetstalkService settledBetLetstalkService,
    SignalRService signalRService,
    SendLetstalkServiceStage letstalkServiceStage,
    LoggingService loggingService,
    TechnicalErrorProviderMistakeLetstalkService technicalErrorProviderMistakeLetstalkService)
    : SendLetstalkService
{
    public async Task Execute()
    {
        var automationInfoList = await letstalkServiceStage.GetVoidBetAutomationRunLetstalk();
        if (!automationInfoList.Any())
        {
            return;
        }

        var groupRequests = automationInfoList.GroupBy(x => x.RequestId).Select(x => new GroupVoidBetAutomationInfo
        {
            RequestId = x.Key,
            VoidBetAutomationList = x
        });

        foreach (var request in groupRequests)
        {
            await RunLetstalkForSingleRequest(request);
        }
    }

    private async Task RunLetstalkForSingleRequest(GroupVoidBetAutomationInfo group)
    {
        var stopwatch = Stopwatch.StartNew();
        group.RequestInfo = await voidBetAutomationService.GetRequestViewModelById(group.RequestId, group.MatchId);
        if (Helper.IsCanSendLetstalk(group.RequestInfo.Request.IssueTypeId))
        {
            var voidedTickets = await letstalkServiceStage.GetVoidedTickets(group.VoidBetAutomationList);
            var matchInfo = await voidBetAutomationService.GetMatchInfo(group.MatchId);
            var voidDataXmlList = await letstalkServiceStage.GetVoidDataXml(group.VoidBetAutomationList.Select(x => x.VoidingHistoryId));
            var sentLetstalkResults = await SendLetstalk(group, voidedTickets, matchInfo, voidDataXmlList);
            await letstalkServiceStage.WriteLogLetstalk(sentLetstalkResults, group, stopwatch);
        }
    }

    private async Task<IEnumerable<Result>> SendLetstalk(GroupVoidBetAutomationInfo group,
        IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, IEnumerable<VoidDataXmlModel> voidDataXmlList)
    {
        try
        {
            var notificationLetstalkModel = CreateNotificationLetstalkModel(group, matchInfo, voidDataXmlList, emailTickets);
            var sendResults = await SendAllLetstalkNotifications(group, notificationLetstalkModel, emailTickets);
            
            await letstalkServiceStage.UpdateLetstalk(group.RequestId, group.BroadcastSessionId, ExecuteStatus.Done);
            
            return sendResults.Any() ? sendResults : new List<Result>();
        }
        catch (Exception exception)
        {
            await HandleSendLetstalkError(group, exception);
            return new List<Result>();
        }
    }

    private async Task<IEnumerable<Result>> SendAllLetstalkNotifications(
        GroupVoidBetAutomationInfo group,
        NotificationLetstalkModel model,
        IEnumerable<TicketEmailModel> emailTickets)
    {
        var results = new List<Result>();
        var sendResults = new Dictionary<string, bool>
        {
            { "SettledBet", settledBetLetstalkService.Send(model, emailTickets) },
            { "AbnormalBet", await SendAbnormalBetIfNeeded(group, model, emailTickets) },
            { "TechnicalError", SendTechnicalErrorIfNeeded(group, model, emailTickets) }
        };

        foreach (var result in sendResults.Where(x => !x.Value))
        {
            results.Add(new Result 
            { 
                ErrorCode = ErrorCode.SendLetstalkFail, 
                ErrorMsg = $"Failed to send {result.Key} message via Letstalk." 
            });
        }

        return results;
    }

    private async Task<bool> SendAbnormalBetIfNeeded(
        GroupVoidBetAutomationInfo group,
        NotificationLetstalkModel model,
        IEnumerable<TicketEmailModel> emailTickets)
    {
        if (!Helper.IsAbnormalBet(group.RequestInfo.Request.IssueTypeId))
        {
            return true;
        }

        return await abnormalBetLetstalkServiceImp.Send(model, emailTickets);
    }

    private bool SendTechnicalErrorIfNeeded(
        GroupVoidBetAutomationInfo group,
        NotificationLetstalkModel model,
        IEnumerable<TicketEmailModel> emailTickets)
    {
        if (!IsTechnicalErrorProviderMistake(group.RequestInfo))
        {
            return true;
        }

        return technicalErrorProviderMistakeLetstalkService.Send(model, emailTickets);
    }

    private async Task HandleSendLetstalkError(GroupVoidBetAutomationInfo group, Exception exception)
    {
        await loggingService.ErrorAsync(
            $"ERROR: RequestId: {group.RequestId} - {nameof(SendLetstalk)} - BroadcastSessionId: {group.BroadcastSessionId}",
            exception);
            
        await letstalkServiceStage.UpdateLetstalk(group.RequestId, group.BroadcastSessionId, ExecuteStatus.Failed);
        await signalRService.UpdateFailedStatusForRequest(group.RequestId);
    }

    private static NotificationLetstalkModel CreateNotificationLetstalkModel(GroupVoidBetAutomationInfo group,
        Match matchInfo, IEnumerable<VoidDataXmlModel> voidDataXmlList, IEnumerable<TicketEmailModel> emailTickets)
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

    private static bool IsTechnicalErrorProviderMistake(RequestDetailViewModel requestInfo)
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