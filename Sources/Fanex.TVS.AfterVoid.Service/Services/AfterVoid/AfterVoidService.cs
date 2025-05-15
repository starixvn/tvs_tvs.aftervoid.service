using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoRecover;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface AfterVoidService
{
    Task Execute();
}

public class AfterVoidServiceImp(VoidBetAutomationService voidBetAutomationService,
    RequestAutoVoidService requestAutoVoidService,
    RequestManualVoidService requestManualVoidService,
    RequestVoid14Service requestVoid14Service,
    RequestAutoRecoverService requestAutoRecoverService,
    LoggingService loggingService)
    : AfterVoidService
{
    public async Task Execute()
    {
        var stopwatch = Stopwatch.StartNew();
        var (requestsWillRunAfterVoid, isLastedInRequest, currentRequestStatus, emailResultsHistory) = await voidBetAutomationService.GetVoidBetAutomationRunAfterVoid();

        if (requestsWillRunAfterVoid == null)
        {
            return;
        }

        await UpdateRequestInfo(requestsWillRunAfterVoid);

        if (requestsWillRunAfterVoid.IsAutoVoid)
        {
            await ExecuteForAutoQueue(requestsWillRunAfterVoid, isLastedInRequest, currentRequestStatus);
        }
        else
        {
            await ExecuteForManualVoid(requestsWillRunAfterVoid.RequestInfo.Request.IssueTypeId, requestsWillRunAfterVoid, isLastedInRequest, currentRequestStatus, emailResultsHistory);
        }

        await loggingService.InfoAsync(
             $"End - BroadcastSessionId: {requestsWillRunAfterVoid.BroadcastSessionId} - "
             + $"Execute Time: {stopwatch.ElapsedMilliseconds} ms - "
             + $"{DateTimeOffset.Now}"
         );
    }

    private async Task UpdateRequestInfo(VoidBetAutomationInfo automationInfo)
    {
        automationInfo.RequestInfo = await voidBetAutomationService.GetRequestViewModelById(automationInfo.RequestId, automationInfo.MatchId);
    }

    private async Task ExecuteForAutoQueue(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        if (automationInfo.RequestInfo.Request.ActionId == VoidActionTypeConstant.AutoRecoverInstant)
        {
            await requestAutoRecoverService.Execute(automationInfo, isLastedInRequest, currentRequestStatus);
        }
        else
        {
            await requestAutoVoidService.Execute(automationInfo, isLastedInRequest, currentRequestStatus);
        }
    }

    private async Task ExecuteForManualVoid(int issueTypeId, VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus,
        IEnumerable<EmailResultModel> emailResultsHistory)
    {
        if (Helper.IsArchivedTicket(issueTypeId))
        {
            await requestVoid14Service.Execute(automationInfo, isLastedInRequest, currentRequestStatus);
        }
        else
        {
            await requestManualVoidService.Execute(automationInfo, isLastedInRequest, currentRequestStatus, emailResultsHistory);
        }
    }
}