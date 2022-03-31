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
                await RunRequestOneByOne(automationInfo, firstRequest.MatchId, matchInfo, message, voidDataXml);
            }
        }

        public async Task RunRequestOneByOne(VoidBetAutomationInfo automationInfo, int matchId, Match matchInfo, Message message, VoidDataXmlModel voidDataXml)
        {
            if (automationInfo.StatusId == ExecuteStatus.WinServiceDone)
            {
                return;
            }

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
            var results = new List<Result>();
            results.AddRange(sentEmailResults);
            await handleAfterVoidStepService.UpdateEmailResult(automationInfo, sentEmailResults);

            if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                var (isSentLetstalk, sentLetstalkResults) = await handleAfterVoidStepService.SendLetstalk(automationInfo, voidedTickets, matchInfo, voidDataXml);
                results.AddRange(sentLetstalkResults);
                if (!isSentLetstalk)
                {
                    notificationService.NotifyException(voidDataXml.CreatorName);
                    return;
                }
            }

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceDone);

            results.Insert(0, new Result { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = GetMessageVoidRequestSuccess(automationInfo.RequestId, matchId) });

            await notificationService.NotifyCompletedRequests(automationInfo.RequestId, automationInfo.VoidingHistory);
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

        private static string GetMessageVoidRequestSuccess(int requestId, int matchId)
        {
            if (matchId > 0)
            {
                return $"Request ID: {requestId} Match ID: {matchId} voided successfully!";
            }

            return $"Request ID: {requestId} voided successfully!";
        }
    }
}