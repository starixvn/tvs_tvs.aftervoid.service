using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly MainProcessStageService mainProcessStageService;
    private readonly CallCashOutApiStage callCashOutApiStage;
    private readonly WriteLogAfterVoidStage writeLogAfterVoidStage;
    private readonly NotifyVoidResultsSignalRStage notifyVoidResultsSignalRStage;
    private readonly CreateVoidReasonStage createVoidReasonStage;

    public RequestVoid14ServiceImp(VoidBetAutomationService voidBetAutomationService, MainProcessStageService mainProcessStageService,
        NotificationSignalRService notificationSignalRService, CallCashOutApiStage callCashOutApiStage,
        WriteLogAfterVoidStage writeLogAfterVoidStage, NotifyVoidResultsSignalRStage notifyVoidResultsSignalRStage,
        CreateVoidReasonStage createVoidReasonStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.voidBetAutomationService = voidBetAutomationService;
        this.mainProcessStageService = mainProcessStageService;
        this.notificationSignalRService = notificationSignalRService;
        this.callCashOutApiStage = callCashOutApiStage;
        this.writeLogAfterVoidStage = writeLogAfterVoidStage;
        this.notifyVoidResultsSignalRStage = notifyVoidResultsSignalRStage;
        this.createVoidReasonStage = createVoidReasonStage;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await ExecuteStepByStep(automationInfo, isLastedInRequest, currentRequestStatus);
        }
    }

    private async Task ExecuteStepByStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var firstVoidData = allVoidDataInCurrentVoid.First();
        var voidReason = await createVoidReasonStage.Execute(new CreateVoidReasonStageParameter
        {
            AutomationInfo = automationInfo,
            SportTypeId = 0,
            VoidData = firstVoidData
        });
        var isContinueInsertVoidedTransaction = !Helper.IsNotHaveVoidedReason(voidReason);
        var isInsertedVoidedTransaction = await mainProcessStageService.InsertVoidedTransaction(isContinueInsertVoidedTransaction,
            new InsertVoidedTransactionStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = voidedTickets,
                SpecialTickets = null,
                NormalMessageId = 0,
                SpecialMessageId = 0,
                VoidReasonId = voidReason?.ReasonId ?? 0
            });
        var isUpdatedVoidingHistory = await mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction,
            new UpdateTicketVoidingHistoryStageParameter
            {
                AutomationInfo = automationInfo,
                NormalMessageId = 0,
                SpecialMessageId = 0,
                VoidReasonId = voidReason?.ReasonId ?? 0,
                EmailTickets = voidedTickets
            });

        if (!isUpdatedVoidingHistory)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        await callCashOutApiStage.Execute(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await CallSignalR(automationInfo, isLastedInRequest, currentRequestStatus, firstVoidData.CreatorName);
        await writeLogAfterVoidStage.Execute(automationInfo, new List<Result>());
    }

    private async Task CallSignalR(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, string creatorName)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            await notifyVoidResultsSignalRStage.Execute(automationInfo, new List<Result>(), creatorName, new List<EmailResultModel>());
        }
    }
}