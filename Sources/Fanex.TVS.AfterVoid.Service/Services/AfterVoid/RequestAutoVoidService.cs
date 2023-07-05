using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

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

    public RequestAutoVoidServiceImp(VoidBetAutomationService voidBetAutomationService, AfterVoidProcessStage afterVoidProcessStage,
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
        var issueTypeId = automationInfo.RequestInfo.Request.IssueTypeId;
        var filterBy = automationInfo.RequestInfo.EventByMatch.FilterBy;
        var matchInfo = await GetMatch(automationInfo);
        var voidDataXml = await GetVoidDataXml(automationInfo);

        var message = await afterVoidProcessStage.CreateMessageIdForAutoVoid(automationInfo, matchInfo, automationInfo.BroadcastSessionId, voidDataXml, issueTypeId, filterBy);
        if (Helper.IsNotHaveMessage(message))
        {
            return;
        }

        var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, GetSportTypeId(matchInfo), null);
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            return;
        }

        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var isSentBatchMessage = await afterVoidProcessStage.SendBatchMessage(automationInfo, voidedTickets, message.MessageId, null);
        if (!isSentBatchMessage)
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

        var isInsertedVoidedTransaction = await afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId);
        if (!isInsertedVoidedTransaction)
        {
            return;
        }

        var isUpdatedVoidingHistory = await afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets);
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

    private async Task<VoidDataXmlModel> GetVoidDataXml(VoidBetAutomationInfo automationInfo)
    {
        var issueTypesAutoVoidActionHaveNotVoidData = new List<int>
        {
            IssueTypeConstant.AbnormalBet,
            IssueTypeConstant.AbnormalBetRealMember
        };
        return issueTypesAutoVoidActionHaveNotVoidData.Contains(automationInfo.RequestInfo.Request.IssueTypeId)
            ? new VoidDataXmlModel()
            : await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
    }
}