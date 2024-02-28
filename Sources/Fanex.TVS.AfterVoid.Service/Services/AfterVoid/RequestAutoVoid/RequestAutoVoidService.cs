using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;

public interface RequestAutoVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestAutoVoidServiceImp : RequestAutoVoidService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly MainProcessStageService mainProcessStageService;
    private readonly UpdateEmailResultStage updateEmailResultStage;
    private readonly CallCashOutApiStage callCashOutApiStage;
    private readonly WriteLogAfterVoidStage writeLogAfterVoidStage;
    private readonly CallSignalRStage callSignalRStage;
    private readonly SpecialPlatformService specialPlatformService;

    public RequestAutoVoidServiceImp(VoidBetAutomationService voidBetAutomationService, MainProcessStageService mainProcessStageService,
        UpdateEmailResultStage updateEmailResultStage, CallCashOutApiStage callCashOutApiStage, WriteLogAfterVoidStage writeLogAfterVoidStage,
        CallSignalRStage callSignalRStage, SpecialPlatformService specialPlatformService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.mainProcessStageService = mainProcessStageService;
        this.updateEmailResultStage = updateEmailResultStage;
        this.callCashOutApiStage = callCashOutApiStage;
        this.writeLogAfterVoidStage = writeLogAfterVoidStage;
        this.callSignalRStage = callSignalRStage;
        this.specialPlatformService = specialPlatformService;
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
        var matchInfo = await mainProcessStageService.GetMatchInfo(automationInfo.MatchId);
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);

        var (normalMessageId, specialMessageId, normalMessageContent) = await mainProcessStageService.CreateMessage(
            new CreateMessageStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets
            });

        var voidReason = await mainProcessStageService.CreateVoidReason(mainProcessStageService.IsHaveMessage(normalMessageId, specialMessageId),
            new CreateVoidReasonStageParameter
            {
                AutomationInfo = automationInfo,
                SportTypeId = GetSportTypeId(matchInfo),
                VoidData = null
            });

        var isSendBatchMessage = await mainProcessStageService.SendBatchMessage(mainProcessStageService.IsHaveVoidReason(voidReason),
            new SendBatchMessageStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidData = null
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
            return;
        }

        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.Execute(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForAutoVoid(isLastedInRequest, currentRequestStatus, automationInfo);
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }

    private static int GetSportTypeId(Match matchInfo)
    {
        return matchInfo?.SportType ?? 0;
    }
}