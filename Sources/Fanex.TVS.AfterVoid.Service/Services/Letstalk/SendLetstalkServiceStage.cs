using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk
{
    public interface SendLetstalkServiceStage
    {
        Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk();

        Task UpdateSendLetstalkStep(int requestId, string broadcastSessionId, VoidBetAutomationInfo afterVoidHistory);

        Task UpdateServiceStatusBySessionAndRequest(int requestId, string broadcastSessionId, int statusId);

        Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, int voidingHistoryId);

        Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList);

        Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds);

        void WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group);
    }

    public class SendLetstalkServiceStageImp : SendLetstalkServiceStage
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly IDynamicRepository repository;
        private readonly NotificationSignalRService notificationSignalRService;
        private readonly RequestVoid14Service requestVoid14Service;
        private readonly LoggingService loggingService;
        private readonly TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService;

        public SendLetstalkServiceStageImp(VoidBetAutomationService voidBetAutomationService,
            IDynamicRepository repository,
            NotificationSignalRService notificationSignalRService,
            RequestVoid14Service requestVoid14Service,
            LoggingService loggingService,
            TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.repository = repository;
            this.notificationSignalRService = notificationSignalRService;
            this.requestVoid14Service = requestVoid14Service;
            this.loggingService = loggingService;
            this.ticketVoidingSystemHttpClientService = ticketVoidingSystemHttpClientService;
        }

        public async Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk()
        {
            var criteria = new GetAllVoidBetAutomationInfoCriteria
            {
                Mode = 2
            };
            return await repository.FetchAsync<VoidBetAutomationInfo>(criteria);
        }

        public async Task UpdateSendLetstalkStep(int requestId, string broadcastSessionId, VoidBetAutomationInfo afterVoidHistory)
        {
            await voidBetAutomationService.UpdateVoidBetAutomationServiceInfo(
                VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep, broadcastSessionId, requestId, null, null,
                afterVoidHistory);
        }

        public async Task UpdateServiceStatusBySessionAndRequest(int requestId, string broadcastSessionId, int statusId)
        {
            await voidBetAutomationService.UpdateVoidBetAutomationServiceInfo(
                VoidBetAutomationServiceUpdateType.UpdateStatusBySession, broadcastSessionId, requestId, null, statusId,
                null);
        }

        public async Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, int voidingHistoryId)
        {
            var (isSentEmail, results) = await ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket(requestId, matchId, issueTypeId, voidingHistoryId.ToString());
            if (isSentEmail)
            {
                return results;
            }

            throw new ExternalException("ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket");
        }

        public async Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList)
        {
            var voidedTickets = new List<TicketEmailModel>();
            foreach (var item in voidBetAutomationList)
            {
                var ticketsInServer =
                    await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(item.VoidingHistoryId,
                        item.SiteDbGroupId);
                voidedTickets.AddRange(ticketsInServer);
            }

            return voidedTickets;
        }

        public async Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds)
        {
            var criteria = new GetVoidingHistoryCriteria
            {
                VoidingHistoryIds = string.Join(",", voidingHistoryIds)
            };

            return await repository.FetchAsync<VoidDataXmlModel>(criteria);
        }

        public void WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"SendLetstalk - BroadcastSessionId: {group.BroadcastSessionId} - RequestId: {group.RequestId} \n");
            foreach (var result in results)
            {
                var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
                logBuilder.Append(log);
            }

            loggingService.Info(logBuilder.ToString());
        }
    }
}