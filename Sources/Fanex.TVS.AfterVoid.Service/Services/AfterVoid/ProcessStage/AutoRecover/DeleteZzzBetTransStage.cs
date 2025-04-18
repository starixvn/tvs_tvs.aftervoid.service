using System;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;

public interface DeleteZzzBetTransStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, string transIds);
}

public class DeleteZzzBetTransStageImp(
    IDynamicRepository repository,
    VoidBetAutomationService voidBetAutomationService,
    SignalRService signalRService,
    LoggingService loggingService)
    : DeleteZzzBetTransStage
{
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
        if (automationInfo.IsDeletedZzzBetTrans == true)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(transIds))
        {
            return false;
        }

        await ExecuteDeleteZzzBetTrans(transIds, automationInfo.RequestInfo.EventByMatch.FilterBy);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId,
            new VoidBetAutomationInfo { IsDeletedZzzBetTrans = true });

        return true;
    }

    private async Task ExecuteDeleteZzzBetTrans(string transIds, int? filterBy)
    {
        var tasks = SiteDbGroupConstant.ServerList.Select(x => ExecuteDeleteZzzBetTransByServer(x.Id, transIds, filterBy));
        await Task.WhenAll(tasks);
    }

    private async Task ExecuteDeleteZzzBetTransByServer(int siteDbGroupId, string transIds, int? filterBy)
    {
        var command = new DeleteZzzBetTransCommand
        {
            SiteDbGroupId = siteDbGroupId,
            TransIds = transIds,
            Type = filterBy
        };
        await repository.ExecuteAsync(command);
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(DeleteZzzBetTransStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
            exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await signalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}