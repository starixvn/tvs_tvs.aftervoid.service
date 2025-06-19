using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
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

        var groupRequests = automationInfoList
            .GroupBy(x => x.RequestId)
            .Select(x => new GroupVoidBetAutomationInfo
            {
                RequestId = x.Key,
                VoidBetAutomationList = x
            });

        foreach (var request in groupRequests)
        {
            await ProcessSingleRequest(request);
        }
    }

    private async Task ProcessSingleRequest(GroupVoidBetAutomationInfo group)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            group.RequestInfo = await voidBetAutomationService.GetRequestViewModelById(group.RequestId, group.MatchId);
            
            if (!Helper.IsCanSendLetstalk(group.RequestInfo.Request.IssueTypeId))
            {
                return;
            }

            var voidedTickets = await letstalkServiceStage.GetVoidedTickets(group.VoidBetAutomationList);
            var matchInfo = await voidBetAutomationService.GetMatchInfo(group.MatchId);
            var voidDataXmlList = await letstalkServiceStage.GetVoidDataXml(group.VoidBetAutomationList.Select(x => x.VoidingHistoryId));
            
            var notificationModel = letstalkServiceStage.CreateNotificationModel(group, matchInfo, voidDataXmlList, voidedTickets);
            var sendResults = await SendAllLetstalkNotifications(group, notificationModel, voidedTickets);
            
            await letstalkServiceStage.UpdateLetstalk(group.RequestId, group.BroadcastSessionId, ExecuteStatus.Done);
            await letstalkServiceStage.WriteLogLetstalk(sendResults, group, stopwatch);
        }
        catch (Exception exception)
        {
            await HandleProcessError(group, exception, stopwatch);
        }
    }

    private async Task<IEnumerable<Result>> SendAllLetstalkNotifications(
        GroupVoidBetAutomationInfo group,
        NotificationLetstalkModel model,
        IEnumerable<TicketEmailModel> emailTickets)
    {
        var sendResults = new Dictionary<string, bool>
        {
            { "SettledBet", settledBetLetstalkService.Send(model, emailTickets) },
            { "AbnormalBet", await SendAbnormalBetIfNeeded(group, model, emailTickets) },
            { "TechnicalError", SendTechnicalErrorIfNeeded(group, model, emailTickets) }
        };

        return sendResults.Where(x => !x.Value)
            .Select(result => new Result { ErrorCode = ErrorCode.SendLetstalkFail, ErrorMsg = $"Failed to send {result.Key} message via Letstalk." }).ToList();
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
        if (!letstalkServiceStage.IsTechnicalErrorProviderMistake(group.RequestInfo))
        {
            return true;
        }

        return technicalErrorProviderMistakeLetstalkService.Send(model, emailTickets);
    }

    private async Task HandleProcessError(GroupVoidBetAutomationInfo group, Exception exception, Stopwatch stopwatch)
    {
        await loggingService.ErrorAsync(
            $"ERROR: RequestId: {group.RequestId} - {nameof(ProcessSingleRequest)} - BroadcastSessionId: {group.BroadcastSessionId}",
            exception);
            
        await letstalkServiceStage.UpdateLetstalk(group.RequestId, group.BroadcastSessionId, ExecuteStatus.Failed);
        await signalRService.UpdateFailedStatusForRequest(group.RequestId);
        await letstalkServiceStage.WriteLogLetstalk([], group, stopwatch);
    }
}