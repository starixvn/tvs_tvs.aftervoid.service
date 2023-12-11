using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendBatchMessageStage sendBatchMessageStage;
    private readonly CreateMessageStage createMessageStage;
    private readonly SpecialPlatformService specialPlatformService;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;

    public RequestAutoVoidServiceImp(VoidBetAutomationService voidBetAutomationService, AfterVoidProcessStage afterVoidProcessStage, NotificationSignalRService notificationSignalRService,
        SendBatchMessageStage sendBatchMessageStage, CreateMessageStage createMessageStage, SpecialPlatformService specialPlatformService, InsertVoidedTransactionStage insertVoidedTransactionStage,
        UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.afterVoidProcessStage = afterVoidProcessStage;
        this.notificationSignalRService = notificationSignalRService;
        this.sendBatchMessageStage = sendBatchMessageStage;
        this.createMessageStage = createMessageStage;
        this.specialPlatformService = specialPlatformService;
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
        var matchInfo = await GetMatch(automationInfo);
        var allVoidDataInCurrentVoidTimes = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);

        var (normalMessageId, specialMessageId) = await createMessageStage.Execute(automationInfo, matchInfo, allVoidDataInCurrentVoidTimes, normalTickets, specialTickets);

        if (normalMessageId == 0 && specialMessageId == 0)
        {
            return;
        }

        var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, GetSportTypeId(matchInfo), null);
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            return;
        }

        var isSendBatchMessage = await sendBatchMessageStage.Execute(automationInfo, normalTickets, specialTickets, normalMessageId, specialMessageId, null);
        if (!isSendBatchMessage)
        {
            return;
        }

        if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            var isSentMessageToAdminSite = await afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message);
            if (!isSentMessageToAdminSite)
            {
                return;
            }
        }

        var isInsertedVoidedTransaction = await insertVoidedTransactionStage.Execute(automationInfo, normalTickets, specialTickets, normalMessageId, specialMessageId, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            return;
        }

        var isUpdatedVoidingHistory = await updateTicketVoidingHistoryStage.Execute(automationInfo, normalMessageId, specialMessageId, voidedReason.ReasonId, voidedTickets);
        if (!isUpdatedVoidingHistory)
        {
            return;
        }

        var (isSentEmail, currentEmailResults) = await afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message);
        if (!isSentEmail)
        {
            return;
        }

        await afterVoidProcessStage.UpdateEmailResult(automationInfo, currentEmailResults);

        await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await ExecutePostMainStep(automationInfo, isLastedInRequest, currentRequestStatus, currentEmailResults);
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