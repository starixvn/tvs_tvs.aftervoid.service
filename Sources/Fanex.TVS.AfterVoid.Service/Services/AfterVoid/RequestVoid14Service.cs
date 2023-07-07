using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface RequestVoid14Service
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestVoid14ServiceImp : RequestVoid14Service
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;

    public RequestVoid14ServiceImp(VoidBetAutomationService voidBetAutomationService,
        AfterVoidProcessStage afterVoidProcessStage,
        NotificationSignalRService notificationSignalRService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.afterVoidProcessStage = afterVoidProcessStage;
        this.notificationSignalRService = notificationSignalRService;
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

        var isInsertedVoidedTransaction = await afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var isUpdatedVoidingHistory = await afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets);
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