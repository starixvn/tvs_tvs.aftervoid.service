using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CreateVoidReasonStage
{
    Task<VoidedReason> Execute(CreateVoidReasonStageParameter parameter);
}

public class CreateVoidReasonStageImp(
    VoidBetAutomationService voidBetAutomationService,
    SendMessageService sendMessageService,
    NotificationSignalRService notificationSignalRService,
    LoggingService loggingService)
    : CreateVoidReasonStage
{
    public async Task<VoidedReason> Execute(CreateVoidReasonStageParameter parameter)
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

    private async Task<VoidedReason> TryExecute(CreateVoidReasonStageParameter parameter)
    {
        VoidedReason reason;
        if (parameter.AutomationInfo.ReasonId > 0)
        {
            reason = await sendMessageService.GetVoidedReason(parameter.AutomationInfo.ReasonId.Value);
        }
        else
        {
            reason = await sendMessageService.InitializeVoidReason(parameter.AutomationInfo.IsAutoVoid, parameter.AutomationInfo.RequestInfo.EventByMatch.SubIssueTypeId,
                parameter.SportTypeId, parameter.VoidData);
            reason.ReasonId = await sendMessageService.InsertReason(reason.ReasonEnglish, reason.ReasonChineseSimplified);
            await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId,
            new VoidBetAutomationInfo { ReasonId = reason.ReasonId });
        }

        return reason;
    }

    private async Task<VoidedReason> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(CreateVoidReasonStage)}.{nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId}" +
            $" - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return null;
    }
}