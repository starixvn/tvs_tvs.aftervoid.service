using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using TicketVoidingSystem.Constants;

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
    private readonly CallSignalRStage callSignalRStage;

    public RequestManualVoidServiceImp(
        VoidBetAutomationService voidBetAutomationService, MainProcessStageService mainProcessStageService,
        NotificationSignalRService notificationSignalRService, SpecialPlatformService specialPlatformService,
        UpdateEmailResultStage updateEmailResultStage, CallCashOutApiStage callCashOutApiStage,
        WriteLogAfterVoidStage writeLogAfterVoidStage, CallSignalRStage callSignalRStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.mainProcessStageService = mainProcessStageService;
        this.notificationSignalRService = notificationSignalRService;
        this.specialPlatformService = specialPlatformService;
        this.updateEmailResultStage = updateEmailResultStage;
        this.callCashOutApiStage = callCashOutApiStage;
        this.writeLogAfterVoidStage = writeLogAfterVoidStage;
        this.callSignalRStage = callSignalRStage;
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
        var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);

        var (normalMessageId, specialMessageId, normalMessageContent, specialMessageContent) = await mainProcessStageService.CreateMessage(
            new CreateMessageStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                VoidDataXml = voidDataXml,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets
            });

        var voidReason = await mainProcessStageService.CreateVoidReason(mainProcessStageService.IsHaveMessage(normalMessageId, specialMessageId),
            new CreateVoidReasonStageParameter
            {
                AutomationInfo = automationInfo,
                SportTypeId = 0,
                VoidData = voidDataXml
            });

        var isSendBatchMessage = await mainProcessStageService.SendBatchMessage(mainProcessStageService.IsHaveVoidReason(voidReason),
            new SendBatchMessageStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidData = voidDataXml
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
                SpecialMessage = specialMessageContent,
                VoidReason = voidReason,
                EmailTickets = voidedTickets
            });

        if (!isSentEmail)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            return;
        }

        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.ExecuteForVoid(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForManualVoid(new CallSignalRManualVoidParameter
        {
            AutomationInfo = automationInfo,
            IsLastedInRequest = isLastedInRequest,
            CurrentRequestStatus = currentRequestStatus,
            CreatorName = voidDataXml.CreatorName,
            CurrentEmailResults = currentEmailResults,
            EmailResultsHistory = emailResultsHistory
        });
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }
}