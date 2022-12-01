using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
{
    public interface RequestManualVoidService
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestManualVoidServiceImp : RequestManualVoidService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly AfterVoidProcessStage afterVoidProcessStage;
        private readonly NotificationSignalRService notificationSignalRService;

        public RequestManualVoidServiceImp(VoidBetAutomationService voidBetAutomationService, AfterVoidProcessStage afterVoidProcessStage,
            NotificationSignalRService notificationSignalRService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.afterVoidProcessStage = afterVoidProcessStage;
            this.notificationSignalRService = notificationSignalRService;
        }

        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            foreach (var automationInfo in automationInfoList)
            {
                if (automationInfo.StatusId == ExecuteStatus.Done)
                {
                    continue;
                }

                await RunMainSteps(automationInfo);
            }
        }

        private async Task RunMainSteps(VoidBetAutomationInfo automationInfo)
        {
            var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(automationInfo.VoidingHistoryId);
            var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);

            var messageContent = await afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, voidDataXml);
            if (IsNotHaveMessage(messageContent))
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, voidDataXml);
            if (IsNotHaveVoidedReason(voidedReason))
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isSentBatchMessage = await afterVoidProcessStage.SendBatchMessage(automationInfo, voidedTickets, messageContent.MessageId, voidDataXml);
            if (!isSentBatchMessage)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                var isSentMessageToAdminSite = await afterVoidProcessStage.SendMessageToAdminSite(automationInfo, messageContent);
                if (!isSentMessageToAdminSite)
                {
                    await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                    return;
                }
            }

            var isInsertedVoidedTransaction = await afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, messageContent.MessageId, voidedReason.ReasonId);
            if (!isInsertedVoidedTransaction)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isUpdatedVoidingHistory = await afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, messageContent.MessageId, voidedReason.ReasonId, voidedTickets);
            if (!isUpdatedVoidingHistory)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var matchInfo = await GetMatchInfo(automationInfo);
            var (isSentEmail, sentEmailResults) = await afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, messageContent);
            if (!isSentEmail)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }
            await afterVoidProcessStage.UpdateEmailResult(automationInfo, sentEmailResults);

            var (isSentLetstalk, sentLetstalkResults) = await afterVoidProcessStage.SendLetstalk(automationInfo, voidedTickets, matchInfo, voidDataXml);
            if (!isSentLetstalk)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            var results = afterVoidProcessStage.CreateResultList(sentEmailResults, sentLetstalkResults, automationInfo.NegativeBalanceUsers, automationInfo.RequestId, automationInfo.MatchId);
            await notificationSignalRService.NotifyVoidResultToUser(voidDataXml.CreatorName, results);
            afterVoidProcessStage.WriteLogAfterVoid(results, automationInfo);
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