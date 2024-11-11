using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface UpdateTicketVoidingHistoryStage
{
    Task<bool> Execute(UpdateTicketVoidingHistoryStageParameter parameter);
}

public class UpdateTicketVoidingHistoryStageImp(
    VoidBetAutomationService voidBetAutomationService,
    NotificationSignalRService notificationSignalRService,
    LoggingService loggingService)
    : UpdateTicketVoidingHistoryStage
{
    public async Task<bool> Execute(UpdateTicketVoidingHistoryStageParameter parameter)
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

    private async Task<bool> TryExecute(UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (parameter.AutomationInfo.IsUpdatedVoidingHistory == true)
        {
            return true;
        }

        await voidBetAutomationService.UpdateTicketVoidingHistory(parameter.AutomationInfo.VoidingHistoryId, parameter.VoidReasonId, parameter.NormalMessageId, parameter.SpecialMessageId);
        await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
           $"ERROR: {nameof(UpdateTicketVoidingHistoryStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
           $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}