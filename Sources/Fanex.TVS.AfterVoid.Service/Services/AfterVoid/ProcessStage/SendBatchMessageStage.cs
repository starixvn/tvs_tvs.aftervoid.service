using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface SendBatchMessageStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets,
        int normalMessageId, int specialMessageId, VoidDataXmlModel voidData);
}

public class SendBatchMessageStageImp : SendBatchMessageStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendMessageService sendMessageService;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public SendBatchMessageStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
    }

    private async Task SendMessage(IEnumerable<TicketEmailModel> emailTickets, int messageId, int issueTypeId, int creatorId)
    {
        if (messageId != 0 && emailTickets.Any())
        {
            await sendMessageService.SendMessage(emailTickets, messageId, issueTypeId, creatorId);
        }
    }

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets,
        int normalMessageId, int specialMessageId, VoidDataXmlModel voidData)
    {
        try
        {
            if (automationInfo.IsSentBatchMessage == true)
            {
                return true;
            }

            var creatorId = automationInfo.IsAutoVoid ? AutoVoidConstant.ActorId : voidData.CreatorId;
            await SendMessage(normalTickets, normalMessageId, automationInfo.RequestInfo.Request.IssueTypeId, creatorId);
            await SendMessage(specialTickets, specialMessageId, automationInfo.RequestInfo.Request.IssueTypeId, creatorId);

            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentBatchMessage = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }
}