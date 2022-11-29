using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
{
    public interface RequestVoid14Service
    {
        Task Execute(VoidBetAutomationInfo automationInfo);
    }

    public class RequestVoid14ServiceImp : RequestVoid14Service
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly AfterVoidProcessStage afterVoidProcessStage;
        private readonly NotificationSignalRService notificationSignalRService;

        public RequestVoid14ServiceImp(VoidBetAutomationService voidBetAutomationService, AfterVoidProcessStage afterVoidProcessStage, NotificationSignalRService notificationSignalRService)
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
            var voidedTickets = await voidBetAutomationService.GetVoidedTicketsForSendEmail(automationInfo.VoidingHistoryId);
            var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);

            var voidedReason = await afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, voidDataXml);
            if (voidedReason == null || voidedReason.ReasonId == 0)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isInsertedVoidedTransaction = await afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId);
            if (!isInsertedVoidedTransaction)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            var isUpdatedVoidingHistory = await afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets);
            if (!isUpdatedVoidingHistory)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXml.CreatorName);
                return;
            }

            await afterVoidProcessStage.CallCashOutApi(automationInfo, voidedTickets);

            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);

            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
            var results = afterVoidProcessStage.CreateResultList(null, null, automationInfo.NegativeBalanceUsers, automationInfo.RequestId, 0);
            await notificationSignalRService.NotifyVoidResultToUser(voidDataXml.CreatorName, results);
            afterVoidProcessStage.WriteLogAfterVoid(results, automationInfo);
        }
    }
}