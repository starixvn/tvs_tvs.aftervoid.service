using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface InsertVoidedTransactionStage
{
    Task<bool> Execute(InsertVoidedTransactionStageParameter parameter);
}

public class InsertVoidedTransactionStageImp : InsertVoidedTransactionStage
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly SendMessageService sendMessageService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly LoggingService loggingService;

    public InsertVoidedTransactionStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
    }

    public async Task<bool> Execute(InsertVoidedTransactionStageParameter parameter)
    {
        try
        {
            return await TryExecute(parameter);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, parameter.AutomationInfo);
        }
    }

    private async Task<bool> TryExecute(InsertVoidedTransactionStageParameter parameter)
    {
        if (parameter.AutomationInfo.IsInsertedVoidedTransaction == true)
        {
            return true;
        }

        await InsertVoidedTransaction(parameter.NormalTickets, parameter.NormalMessageId, parameter.VoidReasonId,
            parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, parameter.AutomationInfo.RequestInfo.EventByMatch.SubIssueTypeId, parameter.AutomationInfo.MatchId);
        await InsertVoidedTransaction(parameter.SpecialTickets, parameter.SpecialMessageId, parameter.VoidReasonId,
            parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, parameter.AutomationInfo.RequestInfo.EventByMatch.SubIssueTypeId, parameter.AutomationInfo.MatchId);

        await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId,
            new VoidBetAutomationInfo { IsInsertedVoidedTransaction = true });

        return true;
    }

    private async Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> emailTickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId)
    {
        if (emailTickets.Any())
        {
            var voidedTicket = emailTickets.Where(x => !x.IsCancelCashOut);
            await sendMessageService.InsertVoidedTransaction(voidedTicket, messageId, reasonId, issueTypeId, subIssueTypeId, matchId);
        }
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(InsertVoidedTransactionStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}