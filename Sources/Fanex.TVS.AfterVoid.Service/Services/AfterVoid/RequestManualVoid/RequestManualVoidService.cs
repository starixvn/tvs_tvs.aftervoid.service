using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;

public interface RequestManualVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory);
}

public class RequestManualVoidServiceImp : RequestManualVoidService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendBatchMessageStage sendBatchMessageStage;
    private readonly CreateMessageStage createMessageStage;
    private readonly SpecialPlatformService specialPlatformService;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;

    public RequestManualVoidServiceImp(
        VoidBetAutomationService voidBetAutomationService,
        AfterVoidProcessStage afterVoidProcessStage,
        NotificationSignalRService notificationSignalRService, SendBatchMessageStage sendBatchMessageStage, CreateMessageStage createMessageStage, SpecialPlatformService specialPlatformService, InsertVoidedTransactionStage insertVoidedTransactionStage,
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

    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await RunMainSteps(automationInfo, isLastedInRequest, currentRequestStatus, emailResultsHistory);
        }
    }

    private async Task RunMainSteps(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var firstVoidData = allVoidDataInCurrentVoid.First();
        var matchInfo = await GetMatchInfo(automationInfo);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);
        var (normalMessageId, specialMessageId, normalMessageContent) = await createMessageStage.Execute(automationInfo, matchInfo, allVoidDataInCurrentVoid, normalTickets, specialTickets);

        if (normalMessageId == 0 && specialMessageId == 0)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, firstVoidData);
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var isSentBatchMessage = await sendBatchMessageStage.Execute(automationInfo, normalTickets, specialTickets, normalMessageId, specialMessageId, firstVoidData);
        if (!isSentBatchMessage)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            var isSentMessageToAdminSite = await afterVoidProcessStage.SendMessageToAdminSite(automationInfo, normalMessageContent);
            if (!isSentMessageToAdminSite)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
                return;
            }
        }

        var isInsertedVoidedTransaction = await insertVoidedTransactionStage.Execute(automationInfo, normalTickets, specialTickets, normalMessageId, specialMessageId, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var isUpdatedVoidingHistory = await updateTicketVoidingHistoryStage.Execute(automationInfo, normalMessageId, specialMessageId, voidedReason.ReasonId, voidedTickets);
        if (!isUpdatedVoidingHistory)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }

        var (isSentEmail, currentEmailResults) = await afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, normalMessageContent);
        if (!isSentEmail)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(firstVoidData.CreatorName);
            return;
        }
        await afterVoidProcessStage.UpdateEmailResult(automationInfo, currentEmailResults);

        await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

        await ExecutePostMainStep(automationInfo, isLastedInRequest, currentRequestStatus, firstVoidData.CreatorName,
            currentEmailResults, emailResultsHistory);
    }

    private async Task ExecutePostMainStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, string creatorName
        , IEnumerable<Result> currentEmailResults, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            await afterVoidProcessStage.NotifyVoidResultsSignalR(automationInfo, currentEmailResults, creatorName, emailResultsHistory);
        }

        await afterVoidProcessStage.WriteLogAfterVoid(currentEmailResults, automationInfo);
    }

    private async Task<Match> GetMatchInfo(VoidBetAutomationInfo automationInfo)
    {
        return automationInfo.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(automationInfo.MatchId) : null;
    }
}