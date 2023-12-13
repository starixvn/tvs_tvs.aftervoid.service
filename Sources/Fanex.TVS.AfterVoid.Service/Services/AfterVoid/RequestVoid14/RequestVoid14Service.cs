using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;

public interface RequestVoid14Service
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestVoid14ServiceImp : RequestVoid14Service
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;

    public RequestVoid14ServiceImp(VoidBetAutomationService voidBetAutomationService,
        AfterVoidProcessStage afterVoidProcessStage,
        NotificationSignalRService notificationSignalRService, InsertVoidedTransactionStage insertVoidedTransactionStage,
        UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.afterVoidProcessStage = afterVoidProcessStage;
        this.notificationSignalRService = notificationSignalRService;
        this.insertVoidedTransactionStage = insertVoidedTransactionStage;
        this.updateTicketVoidingHistoryStage = updateTicketVoidingHistoryStage;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await RunMainSteps(automationInfo, isLastedInRequest, currentRequestStatus);
        }
    }

    private async Task RunMainSteps(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var allVoidDataInCurrentVoidTimes = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var firstVoidData = allVoidDataInCurrentVoidTimes.First();
        var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, firstVoidData);
        if (voidedReason == null || voidedReason.ReasonId == 0)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var isInsertedVoidedTransaction = await insertVoidedTransactionStage.Execute(automationInfo, voidedTickets, null, 0, 0, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var isUpdatedVoidingHistory = await updateTicketVoidingHistoryStage.Execute(automationInfo, 0, 0, voidedReason.ReasonId, voidedTickets);
        if (!isUpdatedVoidingHistory)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

        await ExecutePostMainStep(automationInfo, isLastedInRequest, currentRequestStatus, firstVoidData.CreatorName);
    }

    private async Task ExecutePostMainStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, string creatorName)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            await afterVoidProcessStage.NotifyVoidResultsSignalR(automationInfo, new List<Result>(), creatorName, new List<EmailResultModel>());
        }

        await afterVoidProcessStage.WriteLogAfterVoid(new List<Result>(), automationInfo);
    }
}