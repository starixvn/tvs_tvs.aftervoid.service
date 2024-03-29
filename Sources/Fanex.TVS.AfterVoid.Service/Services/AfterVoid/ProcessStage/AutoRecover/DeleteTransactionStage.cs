using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;

public interface DeleteTransactionStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds);
}

public class DeleteTransactionStageImp : DeleteTransactionStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly IDynamicRepository repository;

    public DeleteTransactionStageImp(IDynamicRepository repository, VoidBetAutomationService voidBetAutomationService,
        NotificationSignalRService notificationSignalRService, LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
        this.repository = repository;
    }

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds)
    {
        try
        {
            return await TryExecute(automationInfo, betIds);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, automationInfo);
        }
    }

    private async Task<bool> TryExecute(VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds)
    {
        if (automationInfo.IsDeletedVoidedTransaction == true)
        {
            return true;
        }

        var command = new DeleteVoidedTransactionCommand
        {
            TicketsXml = betIds.CreateTicketsXml()
        };
        await repository.ExecuteAsync(command);

        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId,
        new VoidBetAutomationInfo { IsDeletedVoidedTransaction = true });

        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(DeleteTransactionStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}