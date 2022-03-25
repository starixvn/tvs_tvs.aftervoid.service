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
            var matchId = firstRequest.MatchId;
            var matchInfo = matchId > 0 ? await voidBetAutomationService.GetMatchInfo(matchId) : null;
            var issueTypeId = firstRequest.RequestInfo.Request.IssueTypeId;
            var filterBy = firstRequest.RequestInfo.EventByMatch.FilterBy;
            var voidDataXml = Helper.IsLikelyAbnormalBet(issueTypeId) ? new VoidDataXmlModel()
                : await voidBetAutomationService.GetVoidDataFromVoidingHistory(firstRequest.VoidingHistory);

            var message = await handleAfterVoidStepService.CreateMessageIdForAllRequests(automationInfoList, matchInfo, firstRequest.SessionId, voidDataXml.IsHasSettledTicket, issueTypeId, filterBy);
            if (message == null || message.MessageId == 0)
            {
                return;
            }

            foreach (var serviceInfo in automationInfoList)
            {
                var results = new List<Result>();
                var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(serviceInfo.VoidingHistory);

                if (serviceInfo.StatusId == ExecuteStatus.WinServiceDone)
                {
                    continue;
                }

                var voidedReason = await handleAfterVoidStepService.CreateVoidedReason(serviceInfo, matchInfo?.SportType ?? 0, null);
                if (voidedReason == null || voidedReason.ReasonId == 0)
                {
                    continue;
                }

                var isSentBatchMessage = await handleAfterVoidStepService.SendBatchMessage(serviceInfo, voidedTickets, message.MessageId, null);
                if (!isSentBatchMessage)
                {
                    continue;
                }

                if (!Helper.IsLikelyAbnormalBet(issueTypeId))
                {
                    var isSentMessageToAdminSite = await handleAfterVoidStepService.SendMessageToAdminSite(serviceInfo, message);
                    if (!isSentMessageToAdminSite)
                    {
                        continue;
                    }
                }

                var isInsertedVoidedTransaction = await handleAfterVoidStepService.InsertVoidedTransaction(serviceInfo, voidedTickets, message.MessageId, voidedReason.ReasonId);
                if (!isInsertedVoidedTransaction)
                {
                    continue;
                }

                var isUpdatedVoidingHistory = await handleAfterVoidStepService.UpdateTicketVoidingHistory(serviceInfo, message.MessageId, voidedReason.ReasonId);
                if (!isUpdatedVoidingHistory)
                {
                    continue;
                }

                var (isSentEmail, sentEmailResults) = await handleAfterVoidStepService.SendEmail(serviceInfo, voidedTickets, matchInfo, voidedReason, message);
                results.AddRange(sentEmailResults);
                if (!isSentEmail)
                {
                    continue;
                }
                await handleAfterVoidStepService.UpdateEmailResult(serviceInfo, sentEmailResults);

                if (!Helper.IsLikelyAbnormalBet(issueTypeId))
                {
                    var sentLetstalkResults = await handleAfterVoidStepService.SendLetstalk(serviceInfo, voidedTickets, matchInfo, voidDataXml);
                    results.AddRange(sentLetstalkResults.Item2);
                    if (!sentLetstalkResults.Item1)
                    {
                        notificationService.NotifyException(voidDataXml.CreatorName);
                        continue;
                    }
                }

                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(serviceInfo.RequestId, serviceInfo.VoidingHistory, ExecuteStatus.WinServiceDone);
                results.Insert(0, new Result { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = GetMessageVoidRequestSuccess(serviceInfo.RequestId, matchId) });

                await notificationService.NotifyCompletedRequests(serviceInfo.RequestId, serviceInfo.VoidingHistory);
                handleAfterVoidStepService.WriteLogAfterVoid(results, serviceInfo);
            }
        }

        private string GetMessageVoidRequestSuccess(int requestId, int matchId)
        {
            if (matchId > 0)
            {
                return $"Request ID: {requestId} Match ID: {matchId} voided successfully!";
            }

            return $"Request ID: {requestId} voided successfully!";
        }
    }
}