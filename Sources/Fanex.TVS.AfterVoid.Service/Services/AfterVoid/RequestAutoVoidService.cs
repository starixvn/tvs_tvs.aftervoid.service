using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
{
    public interface RequestAutoVoidService
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
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

        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            var firstRequest = automationInfoList.FirstOrDefault();
            var matchInfo = firstRequest.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(firstRequest.MatchId) : null;
            var issueTypeId = firstRequest.RequestInfo.Request.IssueTypeId;
            var filterBy = firstRequest.RequestInfo.EventByMatch.FilterBy;
            var voidDataXml = Helper.IsLikelyAbnormalBet(issueTypeId) || Helper.IsAbnormalAccount(issueTypeId) ? new VoidDataXmlModel()
                : await voidBetAutomationService.GetVoidDataFromVoidingHistory(firstRequest.VoidingHistoryId);

            var message = await afterVoidProcessStage.CreateMessageIdForAllRequests(automationInfoList, matchInfo, firstRequest.SessionId, voidDataXml.IsHasSettledTicket, issueTypeId, filterBy);
            if (message == null || message.MessageId == 0)
            {
                return;
            }

            foreach (var automationInfo in automationInfoList)
            {
                if (automationInfo.StatusId == ExecuteStatus.Done)
                {
                    continue;
                }

                await RunMainSteps(automationInfo, firstRequest.MatchId, matchInfo, message, voidDataXml);
            }
        }

        private async Task RunMainSteps(VoidBetAutomationInfo automationInfo, int matchId, Match matchInfo, Message message, VoidDataXmlModel voidDataXml)
        {
            var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, GetSportTypeId(matchInfo), null);
            if (IsNotHaveVoidedReason(voidedReason))
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

            await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.VoidingHistoryId);
            var results = afterVoidProcessStage.CreateResultList(sentEmailResults, sentLetstalkResults, null, automationInfo.RequestId, matchId);
            afterVoidProcessStage.WriteLogAfterVoid(results, automationInfo);
        }

        private static bool IsNotHaveVoidedReason(VoidedReason voidedReason)
        {
            return voidedReason == null || voidedReason.ReasonId == 0;
        }

        private static int GetSportTypeId(Match matchInfo)
        {
            return matchInfo?.SportType ?? 0;
        }
    }
}