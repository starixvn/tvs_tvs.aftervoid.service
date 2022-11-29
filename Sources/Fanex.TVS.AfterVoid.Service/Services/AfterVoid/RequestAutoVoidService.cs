using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
{
    public interface RequestAutoVoidService
    {
        Task Execute(VoidBetAutomationInfo automationInfo);
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

        public async Task Execute(VoidBetAutomationInfo automationInfo)
        {
            if (automationInfo.StatusId != ExecuteStatus.Done)
            {
                await RunMainSteps(automationInfo);
            }
        }

        private async Task RunMainSteps(VoidBetAutomationInfo automationInfo)
        {
            var issueTypeId = automationInfo.RequestInfo.Request.IssueTypeId;
            var filterBy = automationInfo.RequestInfo.EventByMatch.FilterBy;
            var matchInfo = await GetMatch(automationInfo);
            var voidDataXml = await GetVoidDataXml(automationInfo);

            var message = await afterVoidProcessStage.CreateMessageIdForAutoVoid(automationInfo, matchInfo, automationInfo.SessionId, voidDataXml.IsHasSettledTicket, issueTypeId, filterBy);
            if (Helper.IsNotHaveMessage(message))
            {
                return;
            }

            var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, GetSportTypeId(matchInfo), null);
            if (Helper.IsNotHaveVoidedReason(voidedReason))
            {
                return;
            }

            var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(automationInfo.VoidingHistoryId);
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

            var (isSentEmail, sentEmailResults) = await afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message);
            if (!isSentEmail)
            {
                return;
            }

            await afterVoidProcessStage.UpdateEmailResult(automationInfo, sentEmailResults);

            var (isSentLetstalk, sentLetstalkResults) = await afterVoidProcessStage.SendLetstalk(automationInfo, voidedTickets, matchInfo, voidDataXml);
            if (!isSentLetstalk)
            {
                return;
            }

            var isInsertMessageChatBotTeam = await afterVoidProcessStage.InsertMessageChatBotTeam(message, automationInfo.RequestId, automationInfo.VoidingHistoryId, matchInfo?.MatchId);
            if (!isInsertMessageChatBotTeam)
            {
                return;
            }

            await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            var results = afterVoidProcessStage.CreateResultList(sentEmailResults, sentLetstalkResults, null, automationInfo.RequestId, automationInfo.MatchId);
            afterVoidProcessStage.WriteLogAfterVoid(results, automationInfo);
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
            return Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId)
                   || Helper.IsAbnormalAccount(automationInfo.RequestInfo.Request.IssueTypeId)
                ? new VoidDataXmlModel()
                : await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        }
    }
}