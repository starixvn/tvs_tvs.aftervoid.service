using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services.Main
{
    public interface RequestVoid14Service
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestVoid14ServiceImp : RequestVoid14Service
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly HandleAfterVoidStepService handleAfterVoidStepService;
        private readonly NotificationSignalRService notificationSignalRService;

        public RequestVoid14ServiceImp(VoidBetAutomationService voidBetAutomationService, HandleAfterVoidStepService handleAfterVoidStepService, NotificationSignalRService notificationSignalRService)
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

            var voidedReason = await handleAfterVoidStepService.CreateVoidedReason(automationInfo, 0, voidDataXml);
            if (voidedReason == null || voidedReason.ReasonId == 0)
            {
                notificationSignalRService.NotifyException(voidDataXml.CreatorName);
                return;
            }

            var isInsertedVoidedTransaction = await handleAfterVoidStepService.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId);
            if (!isInsertedVoidedTransaction)
            {
                notificationSignalRService.NotifyException(voidDataXml.CreatorName);
                return;
            }

            var isUpdatedVoidingHistory = await handleAfterVoidStepService.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId);
            if (!isUpdatedVoidingHistory)
            {
                notificationSignalRService.NotifyException(voidDataXml.CreatorName);
                return;
            }

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceDone);

            await notificationSignalRService.NotifyCompletedRequests(automationInfo.RequestId, automationInfo.VoidingHistory);
            var results = handleAfterVoidStepService.CreateResultList(null, null, automationInfo.NegativeBalanceUsers, automationInfo.RequestId, 0);
            notificationSignalRService.NotifyVoidResult(results, voidDataXml.CreatorName);
            handleAfterVoidStepService.WriteLogAfterVoid(results, automationInfo);
        }
    }
}