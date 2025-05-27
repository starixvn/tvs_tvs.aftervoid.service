using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Models;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.Constants;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CallMTSApiStage
{
    Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> voidedTickets);
}

public class CallMTSApiStageImp(
    VoidBetAutomationService voidBetAutomationService,
    MTSHttpClientService mtsHttpClientService,
    LoggingService loggingService) : CallMTSApiStage
{
    //TODO: Phase 2 update IsCallMTSApi to table voidbetautomation 
    public async Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> voidedTickets)
    {
        try
        {
            await TryExecute(automationInfo, voidedTickets);
        }
        catch (Exception exception)
        {
            await CatchException(exception, automationInfo);
        }
    }

    private async Task TryExecute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> voidedTickets)
    {
        if (automationInfo.IsSentToMTS == true)
        {
            return;
        }

        var issueTypeId = automationInfo.RequestInfo?.Request?.IssueTypeId;
        var subIssueTypeId = automationInfo.RequestInfo?.EventByMatch?.SubIssueTypeId;
        if (!(issueTypeId == IssueTypeConstant.TechnicalError &&
              subIssueTypeId == SubIssueTypeConstant.TechnicalError_ProviderMistake))
        {
            return;
        }

        if (!voidedTickets.Any())
        {
            return;
        }

        //TODO: Phase 2: Consider this List Tickets, because MTS just call 1 ticket at a time, if create Void in UI, will have multiple tickets
        foreach (var ticket in voidedTickets)
        {
            var request = new MTSApiRequest { TransId = ticket.TransId };
            var response = await mtsHttpClientService.VoidBetAsync(request);
            await loggingService.InfoAsync(
                $"MTS API Response for TransId {ticket.TransId}: Success={response.Success}, Message={response.Message}");
        }

        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId,
            new VoidBetAutomationInfo { IsSentToMTS = true });
    }

    private async Task CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(CallMTSApiStage)}.{nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
    }
}