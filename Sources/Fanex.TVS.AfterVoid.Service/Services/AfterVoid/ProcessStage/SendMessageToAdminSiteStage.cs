using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface SendMessageToAdminSiteStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, Message messageContent);
}

public class SendMessageToAdminSiteStageImp : SendMessageToAdminSiteStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendMessageService sendMessageService;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public SendMessageToAdminSiteStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
    }

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, Message messageContent)
    {
        try
        {
            return await TryExecute(automationInfo, messageContent);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, automationInfo);
        }
    }

    private async Task<bool> TryExecute(VoidBetAutomationInfo automationInfo, Message messageContent)
    {
        if (automationInfo.IsSentMessageToAdminSite == true)
        {
            return true;
        }

        await sendMessageService.InsertMessageIntoAdminSite(messageContent);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentMessageToAdminSite = true });

        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(SendMessageToAdminSiteStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}