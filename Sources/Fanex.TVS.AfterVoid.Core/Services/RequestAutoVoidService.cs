using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface RequestAutoVoidService
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestAutoVoidServiceImp : RequestAutoVoidService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly HandleAfterVoidStepService handleAfterVoidStepService;
        private readonly NotificationService notificationService;

        public RequestAutoVoidServiceImp(VoidBetAutomationService voidBetAutomationService, HandleAfterVoidStepService handleAfterVoidStepService, NotificationService notificationService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.handleAfterVoidStepService = handleAfterVoidStepService;
            this.notificationService = notificationService;
        }

        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            var firstRequest = automationInfoList.FirstOrDefault();
            var matchInfo = firstRequest.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(firstRequest.MatchId) : null;
            var issueTypeId = firstRequest.RequestInfo.Request.IssueTypeId;
            var filterBy = firstRequest.RequestInfo.EventByMatch.FilterBy;
            var voidDataXml = Helper.IsLikelyAbnormalBet(issueTypeId) ? new VoidDataXmlModel()
                : await voidBetAutomationService.GetVoidDataFromVoidingHistory(firstRequest.VoidingHistory);

            var message = await handleAfterVoidStepService.CreateMessageIdForAllRequests(automationInfoList, matchInfo, firstRequest.SessionId, voidDataXml.IsHasSettledTicket, issueTypeId, filterBy);
            if (message == null || message.MessageId == 0)
            {
                return;
            }

            foreach (var automationInfo in automationInfoList)
            {
                if (automationInfo.StatusId == ExecuteStatus.WinServiceDone)
                {
                    continue;
                }

                await RunMainSteps(automationInfo, firstRequest.MatchId, matchInfo, message, voidDataXml);
            }
        }

        private async Task RunMainSteps(VoidBetAutomationInfo automationInfo, int matchId, Match matchInfo, Message message, VoidDataXmlModel voidDataXml)
        {
            var voidedReason = await handleAfterVoidStepService.CreateVoidedReason(automationInfo, GetSportTypeId(matchInfo), null);
            if (IsNotHaveVoidedReason(voidedReason))
            {
                return;
            }

            var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(automationInfo.VoidingHistory);
            var isSentBatchMessage = await handleAfterVoidStepService.SendBatchMessage(automationInfo, voidedTickets, message.MessageId, null);
            if (!isSentBatchMessage)
            {
                return;
            }

            if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                var isSentMessageToAdminSite = await handleAfterVoidStepService.SendMessageToAdminSite(automationInfo, message);
                if (!isSentMessageToAdminSite)
                {
                    return;
                }
            }

            var isInsertedVoidedTransaction = await handleAfterVoidStepService.InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId);
            if (!isInsertedVoidedTransaction)
            {
                return;
            }

            var isUpdatedVoidingHistory = await handleAfterVoidStepService.UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId);
            if (!isUpdatedVoidingHistory)
            {
                return;
            }

            var (isSentEmail, sentEmailResults) = await handleAfterVoidStepService.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message);
            if (!isSentEmail)
            {
                return;
            }

            await handleAfterVoidStepService.UpdateEmailResult(automationInfo, sentEmailResults);

            IEnumerable<Result> sentLetstalkResults = null;
            if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                bool isSentLetstalk;
                (isSentLetstalk, sentLetstalkResults) = await handleAfterVoidStepService.SendLetstalk(automationInfo, voidedTickets, matchInfo, voidDataXml);
                if (!isSentLetstalk)
                {
                    return;
                }
            }

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceDone);

            await notificationService.NotifyCompletedRequests(automationInfo.RequestId, automationInfo.VoidingHistory);
            var results = handleAfterVoidStepService.CreateResultList(sentEmailResults, sentLetstalkResults, null, automationInfo.RequestId, matchId);
            handleAfterVoidStepService.WriteLogAfterVoid(results, automationInfo);
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