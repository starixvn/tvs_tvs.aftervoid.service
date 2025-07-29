using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.Notification;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;

public interface RequestVoid14Service
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestVoid14ServiceImp(
    VoidBetAutomationService voidBetAutomationService,
    MainProcessStageService mainProcessStageService,
    NotificationService notificationService,
    CallCashOutApiStage callCashOutApiStage,
    WriteLogAfterVoidStage writeLogAfterVoidStage,
    CallSignalRStage callSignalRStage)
    : RequestVoid14Service
{
    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await ExecuteStepByStep(automationInfo, isLastedInRequest, currentRequestStatus);
        }
    }

    private async Task ExecuteStepByStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);

        var voidReason = await mainProcessStageService.CreateVoidReason(true, new CreateVoidReasonStageParameter
        {
            AutomationInfo = automationInfo,
            SportTypeId = 0,
            VoidData = voidDataXml
        });

        var isInsertedVoidedTransaction = await mainProcessStageService.InsertVoidedTransaction(mainProcessStageService.IsHaveVoidReason(voidReason),
            new InsertVoidedTransactionStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = voidedTickets,
                SpecialTickets = [],
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
            notificationService.ExecuteBackground(() => notificationService.InsertNotification(NotificationType.Error, MessageHelper.CreateFailed(automationInfo.RequestId), automationInfo.RequestId));
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            return;
        }

        await callCashOutApiStage.ExecuteForVoid(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForManualVoid(new CallSignalRManualVoidParameter
        {
            AutomationInfo = automationInfo,
            IsLastedInRequest = isLastedInRequest,
            CurrentRequestStatus = currentRequestStatus,
            CreatorName = voidDataXml.CreatorName,
            CurrentEmailResults = new List<Result>(),
            EmailResultsHistory = new List<EmailResultModel>()
        });

        await writeLogAfterVoidStage.Execute(automationInfo, new List<Result>());
    }
}