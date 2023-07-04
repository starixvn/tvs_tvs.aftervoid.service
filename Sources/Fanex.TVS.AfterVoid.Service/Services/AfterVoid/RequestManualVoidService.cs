using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface RequestManualVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory);
}

public class RequestManualVoidServiceImp : RequestManualVoidService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;

    public RequestManualVoidServiceImp(
        VoidBetAutomationService voidBetAutomationService,
        AfterVoidProcessStage afterVoidProcessStage,
        NotificationSignalRService notificationSignalRService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.afterVoidProcessStage = afterVoidProcessStage;
        this.notificationSignalRService = notificationSignalRService;
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
        var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);

        var message = await afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, voidDataXml);
        if (Helper.IsNotHaveMessage(message))
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }

        var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, voidDataXml);
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }

        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var isSentBatchMessage = await afterVoidProcessStage.SendBatchMessage(automationInfo, voidedTickets, message.MessageId, voidDataXml);
        if (!isSentBatchMessage)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }

        if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            var isSentMessageToAdminSite = await afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message);
            if (!isSentMessageToAdminSite)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }
        }

        var isInsertedVoidedTransaction = await afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }

        var isUpdatedVoidingHistory = await afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets);
        if (!isUpdatedVoidingHistory)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }

        var matchInfo = await GetMatchInfo(automationInfo);
        var (isSentEmail, currentEmailResults) = await afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message);
        if (!isSentEmail)
        {
            await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
            return;
        }
        await afterVoidProcessStage.UpdateEmailResult(automationInfo, currentEmailResults);

        await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

        await ExecutePostMainStep(automationInfo, isLastedInRequest, currentRequestStatus, voidDataXml.CreatorName,
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