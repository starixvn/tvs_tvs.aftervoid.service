using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services.Main
{
    public interface RequestManualVoidService
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestManualVoidServiceImp : RequestManualVoidService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly HandleAfterVoidStepService handleAfterVoidStepService;
        private readonly NotificationSignalRService notificationSignalRService;

        public RequestManualVoidServiceImp(VoidBetAutomationService voidBetAutomationService, HandleAfterVoidStepService handleAfterVoidStepService,
            NotificationSignalRService notificationSignalRService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.handleAfterVoidStepService = handleAfterVoidStepService;
            this.notificationSignalRService = notificationSignalRService;
        }

        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            foreach (var automationInfo in automationInfoList)
            {
                if (automationInfo.StatusId == ExecuteStatus.WinServiceDone)
                {
                    continue;
                }

                await RunMainSteps(automationInfo);
            }
        }

        private async Task RunMainSteps(VoidBetAutomationInfo automationInfo)
        {
            var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(automationInfo.VoidingHistory);
            var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistory);

            var messageContent = await handleAfterVoidStepService.CreateMessageIdForManualVoid(automationInfo, voidDataXml);
            if (IsNotHaveMessage(messageContent))
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var voidedReason = await handleAfterVoidStepService.CreateVoidedReason(automationInfo, 0, voidDataXml);
            if (IsNotHaveVoidedReason(voidedReason))
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isSentBatchMessage = await handleAfterVoidStepService.SendBatchMessage(automationInfo, voidedTickets, messageContent.MessageId, voidDataXml);
            if (!isSentBatchMessage)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                var isSentMessageToAdminSite = await handleAfterVoidStepService.SendMessageToAdminSite(automationInfo, messageContent);
                if (!isSentMessageToAdminSite)
                {
                    await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                    return;
                }
            }

            var isInsertedVoidedTransaction = await handleAfterVoidStepService.InsertVoidedTransaction(automationInfo, voidedTickets, messageContent.MessageId, voidedReason.ReasonId);
            if (!isInsertedVoidedTransaction)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isUpdatedVoidingHistory = await handleAfterVoidStepService.UpdateTicketVoidingHistory(automationInfo, messageContent.MessageId, voidedReason.ReasonId);
            if (!isUpdatedVoidingHistory)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var matchInfo = await GetMatchInfo(automationInfo);
            var (isSentEmail, sentEmailResults) = await handleAfterVoidStepService.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, messageContent);
            if (!isSentEmail)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }
            await handleAfterVoidStepService.UpdateEmailResult(automationInfo, sentEmailResults);

            var (isSentLetstalk, sentLetstalkResults) = await handleAfterVoidStepService.SendLetstalk(automationInfo, voidedTickets, matchInfo, voidDataXml);
            if (!isSentLetstalk)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            await handleAfterVoidStepService.CallCashOutApi(automationInfo, voidedTickets);

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceDone);

            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.VoidingHistory);
            var results = handleAfterVoidStepService.CreateResultList(sentEmailResults, sentLetstalkResults, automationInfo.NegativeBalanceUsers, automationInfo.RequestId, automationInfo.MatchId);
            await notificationSignalRService.NotifyVoidResultToUser(voidDataXml.CreatorName, results);
            handleAfterVoidStepService.WriteLogAfterVoid(results, automationInfo);
        }

        private static bool IsNotHaveMessage(Message message)
        {
            return message == null || message.MessageId == 0;
        }

        private static bool IsNotHaveVoidedReason(VoidedReason voidedReason)
        {
            return voidedReason == null || voidedReason.ReasonId == 0;
        }

        private async Task<Match> GetMatchInfo(VoidBetAutomationInfo automationInfo)
        {
            return automationInfo.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(automationInfo.MatchId) : null;
        }
    }
}