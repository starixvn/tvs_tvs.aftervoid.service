using System;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;

public interface DeleteMessageStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, string transIds);
}

public class DeleteMessageStageImp : DeleteMessageStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly IDynamicRepository repository;

    public DeleteMessageStageImp(IDynamicRepository repository, VoidBetAutomationService voidBetAutomationService,
        NotificationSignalRService notificationSignalRService, LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
        this.repository = repository;
    }

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, string transIds)
    {
        try
        {
            return await TryExecute(automationInfo, transIds);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, automationInfo);
        }
    }

    private async Task<bool> TryExecute(VoidBetAutomationInfo automationInfo, string transIds)
    {
        var command = new DeleteMessageCommand
        {
            UserId = AutoVoidConstant.ActorId,
            TransIds = transIds
        };
        await repository.ExecuteAsync(command);

        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId,
        new VoidBetAutomationInfo { IsDeletedMessage = true });

        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(DeleteMessageStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}