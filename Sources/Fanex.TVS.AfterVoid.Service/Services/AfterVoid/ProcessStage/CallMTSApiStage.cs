using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

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
    //TODO: Phase 2 update IsCallMTSApi to table void bet automation
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
        if (automationInfo.IsSentToMTS == true || string.IsNullOrWhiteSpace(AppSettingService.AppSetting.MTSApiBaseUrl))
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

        //TODO: Consider this List Tickets, because MTS just call 1 ticket at a time, if create Void in UI, will have multiple tickets
        var firstTicket = voidedTickets.First();
        var request = new MTSApiRequest { TransId = firstTicket.TransId };
        await mtsHttpClientService.VoidBetAsync(request);

        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId,
            new VoidBetAutomationInfo { IsSentToMTS = true });
    }

    private async Task CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(CallMTSApiStage)}.{nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
    }
}