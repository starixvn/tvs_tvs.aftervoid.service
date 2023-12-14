using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;

public interface RequestManualVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory);
}

public class RequestManualVoidServiceImp : RequestManualVoidService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly MainProcessStageService mainProcessStageService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SpecialPlatformService specialPlatformService;
    private readonly UpdateEmailResultStage updateEmailResultStage;
    private readonly CallCashOutApiStage callCashOutApiStage;
    private readonly WriteLogAfterVoidStage writeLogAfterVoidStage;
    private readonly NotifyVoidResultsSignalRStage notifyVoidResultsSignalRStage;

    public RequestManualVoidServiceImp(
        VoidBetAutomationService voidBetAutomationService, MainProcessStageService mainProcessStageService,
        NotificationSignalRService notificationSignalRService, SpecialPlatformService specialPlatformService,
        UpdateEmailResultStage updateEmailResultStage, CallCashOutApiStage callCashOutApiStage,
        WriteLogAfterVoidStage writeLogAfterVoidStage, NotifyVoidResultsSignalRStage notifyVoidResultsSignalRStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.mainProcessStageService = mainProcessStageService;
        this.notificationSignalRService = notificationSignalRService;
        this.specialPlatformService = specialPlatformService;
        this.updateEmailResultStage = updateEmailResultStage;
        this.callCashOutApiStage = callCashOutApiStage;
        this.writeLogAfterVoidStage = writeLogAfterVoidStage;
        this.notifyVoidResultsSignalRStage = notifyVoidResultsSignalRStage;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await ExecuteStepByStep(automationInfo, isLastedInRequest, currentRequestStatus, emailResultsHistory);
        }
    }

    private async Task ExecuteStepByStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        var matchInfo = await mainProcessStageService.GetMatchInfo(automationInfo.MatchId);
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);
        var firstVoidData = allVoidDataInCurrentVoid.First();

        var (normalMessageId, specialMessageId, normalMessageContent) = await mainProcessStageService.CreateMessage(
            new CreateMessageStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets
            });

        var voidReason = await mainProcessStageService.CreateVoidReason(normalMessageId, specialMessageId,
            new CreateVoidReasonStageParameter
            {
                AutomationInfo = automationInfo,
                SportTypeId = 0,
                VoidData = firstVoidData
            });

        var isSendBatchMessage = await mainProcessStageService.SendBatchMessage(voidReason,
            new SendBatchMessageStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidData = firstVoidData
            });

        var isSentMessageToAdminSite = await mainProcessStageService.SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent);

        var isInsertedVoidedTransaction = await mainProcessStageService.InsertVoidedTransaction(isSentMessageToAdminSite,
            new InsertVoidedTransactionStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason?.ReasonId ?? 0
            });

        var isUpdatedVoidingHistory = await mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction,
            new UpdateTicketVoidingHistoryStageParameter
            {
                AutomationInfo = automationInfo,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason?.ReasonId ?? 0,
                EmailTickets = voidedTickets
            });

        var (isSentEmail, currentEmailResults) = await mainProcessStageService.SendEmail(isUpdatedVoidingHistory,
            new SendEmailStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                NormalMessage = normalMessageContent,
                VoidReason = voidReason,
                EmailTickets = voidedTickets
            });

        if (!isSentEmail)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }
        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.Execute(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await CallSignalR(new NotificationSignalRParameter
        {
            AutomationInfo = automationInfo,
            IsLastedInRequest = isLastedInRequest,
            CurrentRequestStatus = currentRequestStatus,
            CreatorName = firstVoidData.CreatorName,
            CurrentEmailResults = currentEmailResults,
            EmailResultsHistory = emailResultsHistory
        });
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }

    private async Task CallSignalR(NotificationSignalRParameter parameter)
    {
        if (parameter.IsLastedInRequest && parameter.CurrentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.MatchId);
            await notifyVoidResultsSignalRStage.Execute(parameter.AutomationInfo, parameter.CurrentEmailResults, parameter.CreatorName, parameter.EmailResultsHistory);
        }
    }
}