using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface RequestAutoVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestAutoVoidServiceImp : RequestAutoVoidService
{
    private readonly CreateMessageStage createMessageStage;
    private readonly CreateVoidReasonStage createVoidReasonStage;
    private readonly SendBatchMessageStage sendBatchMessageStage;
    private readonly SendMessageToAdminSiteStage sendMessageToAdminSiteStage;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;
    private readonly SendEmailStage sendEmailStage;

    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;

    private readonly SpecialPlatformService specialPlatformService;

    public RequestAutoVoidServiceImp(CreateMessageStage createMessageStage, CreateVoidReasonStage createVoidReasonStage, SendBatchMessageStage sendBatchMessageStage,
        SendMessageToAdminSiteStage sendMessageToAdminSiteStage, InsertVoidedTransactionStage insertVoidedTransactionStage,
        UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage, SendEmailStage sendEmailStage,
        VoidBetAutomationService voidBetAutomationService, AfterVoidProcessStage afterVoidProcessStage, NotificationSignalRService notificationSignalRService,
      SpecialPlatformService specialPlatformService)
    {
        this.createMessageStage = createMessageStage;
        this.createVoidReasonStage = createVoidReasonStage;
        this.sendBatchMessageStage = sendBatchMessageStage;
        this.sendMessageToAdminSiteStage = sendMessageToAdminSiteStage;
        this.insertVoidedTransactionStage = insertVoidedTransactionStage;
        this.updateTicketVoidingHistoryStage = updateTicketVoidingHistoryStage;
        this.sendEmailStage = sendEmailStage;

        this.voidBetAutomationService = voidBetAutomationService;
        this.afterVoidProcessStage = afterVoidProcessStage;
        this.notificationSignalRService = notificationSignalRService;

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
        var matchInfo = await GetMatch(automationInfo);
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);

        var (normalMessageId, specialMessageId, normalMessageContent) = await createMessageStage.Execute(
            new CreateMessageStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets
            });

        var voidReason = await CreateVoidReason(normalMessageId, specialMessageId,
            new CreateVoidReasonStageParameter
            {
                AutomationInfo = automationInfo,
                SportTypeId = GetSportTypeId(matchInfo),
                VoidData = null
            });

        var isSendBatchMessage = await SendBatchMessage(voidReason,
            new SendBatchMessageStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidData = null
            });

        var isSentMessageToAdminSite = await SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent);
        var isInsertedVoidedTransaction = await InsertVoidedTransaction(isSentMessageToAdminSite,
            new InsertVoidedTransactionStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason.ReasonId
            });

        var isUpdatedVoidingHistory = await UpdateTicketVoidingHistory(isInsertedVoidedTransaction,
            new UpdateTicketVoidingHistoryStageParameter
            {
                AutomationInfo = automationInfo,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason.ReasonId,
                EmailTickets = voidedTickets
            });

        var (isSentEmail, currentEmailResults) = await SendEmail(isUpdatedVoidingHistory,
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

        await afterVoidProcessStage.UpdateEmailResult(automationInfo, currentEmailResults);

        await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await ExecutePostMainStep(automationInfo, isLastedInRequest, currentRequestStatus, currentEmailResults);
    }

    private async Task<VoidedReason> CreateVoidReason(int normalMessageId, int specialMessageId, CreateVoidReasonStageParameter parameter)
    {
        if (normalMessageId == 0 && specialMessageId == 0)
        {
            return null;
        }

        return await createVoidReasonStage.Execute(parameter);
    }

    private async Task<bool> SendBatchMessage(VoidedReason voidedReason, SendBatchMessageStageParameter parameter)
    {
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            return false;
        }

        return await sendBatchMessageStage.Execute(parameter);
    }

    private async Task<bool> SendMessageToAdminSite(bool isSendBatchMessage, VoidBetAutomationInfo automationInfo, Message normalMessageContent)
    {
        if (!isSendBatchMessage)
        {
            return false;
        }

        if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            return await sendMessageToAdminSiteStage.Execute(automationInfo, normalMessageContent);
        }

        return true;
    }

    private async Task<bool> InsertVoidedTransaction(bool isSentMessageToAdminSite, InsertVoidedTransactionStageParameter parameter)
    {
        if (!isSentMessageToAdminSite)
        {
            return false;
        }

        return await insertVoidedTransactionStage.Execute(parameter);
    }

    private async Task<bool> UpdateTicketVoidingHistory(bool isInsertedVoidedTransaction, UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (!isInsertedVoidedTransaction)
        {
            return false;
        }

        return await updateTicketVoidingHistoryStage.Execute(parameter);
    }

    private async Task<(bool, IEnumerable<Result>)> SendEmail(bool isUpdatedVoidingHistory, SendEmailStageParameter parameter)
    {
        if (!isUpdatedVoidingHistory)
        {
            return (false, new List<Result>());
        }

        return await sendEmailStage.Execute(parameter);
    }

    private async Task ExecutePostMainStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus
        , IEnumerable<Result> currentEmailResults)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
        }

        await afterVoidProcessStage.WriteLogAfterVoid(currentEmailResults, automationInfo);
    }

    private static int GetSportTypeId(Match matchInfo)
    {
        return matchInfo?.SportType ?? 0;
    }

    private async Task<Match> GetMatch(VoidBetAutomationInfo automationInfo)
    {
        return automationInfo.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(automationInfo.MatchId) : null;
    }
}