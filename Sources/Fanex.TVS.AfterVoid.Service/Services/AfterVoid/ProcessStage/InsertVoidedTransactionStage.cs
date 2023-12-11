using Fanex.TVS.AfterVoid.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface InsertVoidedTransactionStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> normalTickets,
        IEnumerable<TicketEmailModel> specialTickets, int normalMessageId, int specialMessageId, int reasonId);
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

    private async Task InsertVoidedTransaction(IEnumerable<TicketEmailModel> emailTickets, int messageId, int reasonId, int issueTypeId, int subIssueTypeId, int matchId)
    {
        if (emailTickets.Any())
        {
            var voidedTicket = emailTickets.Where(x => !x.IsCancelCashOut);
            await sendMessageService.InsertVoidedTransaction(voidedTicket, messageId, reasonId, issueTypeId, subIssueTypeId, matchId);
        }
    }

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> normalTickets,
        IEnumerable<TicketEmailModel> specialTickets, int normalMessageId, int specialMessageId, int reasonId)
    {
        try
        {
            if (automationInfo.IsInsertedVoidedTransaction == true)
            {
                return true;
            }

            await InsertVoidedTransaction(normalTickets, normalMessageId, reasonId,
                automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.RequestInfo.EventByMatch.SubIssueTypeId, automationInfo.MatchId);
            await InsertVoidedTransaction(specialTickets, specialMessageId, reasonId,
                automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.RequestInfo.EventByMatch.SubIssueTypeId, automationInfo.MatchId);

            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsInsertedVoidedTransaction = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }
}