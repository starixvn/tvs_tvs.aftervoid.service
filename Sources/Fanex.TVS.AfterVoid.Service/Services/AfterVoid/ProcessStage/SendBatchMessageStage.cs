using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface SendBatchMessageStage
{
    Task<bool> Execute(SendBatchMessageStageParameter parameter);
}

public class SendBatchMessageStageImp(
    VoidBetAutomationService voidBetAutomationService,
    SendMessageService sendMessageService,
    NotificationSignalRService notificationSignalRService,
    LoggingService loggingService)
    : SendBatchMessageStage
{
    public async Task<bool> Execute(SendBatchMessageStageParameter parameter)
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

    private async Task<bool> TryExecute(SendBatchMessageStageParameter parameter)
    {
        if (parameter.AutomationInfo.IsSentBatchMessage == true)
        {
            return true;
        }

        var creatorId = parameter.AutomationInfo.IsAutoVoid ? AutoVoidConstant.ActorId : parameter.VoidData.CreatorId;
        await SendBatchMessage(parameter.NormalTickets, parameter.NormalMessageId, parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, creatorId);
        await SendBatchMessage(parameter.SpecialTickets, parameter.SpecialMessageId, parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, creatorId);
        await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentBatchMessage = true });
        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync($"ERROR: {nameof(SendBatchMessageStage)}.{nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId}" +
                                        $" - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }

    private async Task SendBatchMessage(IEnumerable<TicketEmailModel> emailTickets, int messageId, int issueTypeId, int creatorId)
    {
        if (messageId != 0 && emailTickets.Any())
        {
            await sendMessageService.SendMessage(emailTickets, messageId, issueTypeId, creatorId);
        }
    }
}