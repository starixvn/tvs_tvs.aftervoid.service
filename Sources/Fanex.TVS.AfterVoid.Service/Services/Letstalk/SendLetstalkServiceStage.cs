using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk
{
    public interface SendLetstalkServiceStage
    {
        Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk();

        Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, int voidingHistoryId);

        Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList);

        Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds);

        void WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime);

        Task InsertLetstalk(int requestId, string broadcastSessionId, int statusId);

        Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId);
    }

    public class SendLetstalkServiceStageImp : SendLetstalkServiceStage
    {
        private readonly IDynamicRepository repository;
        private readonly LoggingService loggingService;
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService;

        public SendLetstalkServiceStageImp(
            IDynamicRepository repository,
            LoggingService loggingService,
            VoidBetAutomationService voidBetAutomationService,
            TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService)
        {
            this.repository = repository;
            this.loggingService = loggingService;
            this.voidBetAutomationService = voidBetAutomationService;
            this.ticketVoidingSystemHttpClientService = ticketVoidingSystemHttpClientService;
        }

        public async Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk()
        {
            var criteria = new GetAutomationInfoLetstalkCriteria();

            return await repository.FetchAsync<VoidBetAutomationInfo>(criteria);
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

        public void WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"SendLetstalk - BroadcastSessionId: {group.BroadcastSessionId} - " +
                              $"RequestId: {group.RequestId} - Execute Time: {startTime.ElapsedMilliseconds} ms \n");
            foreach (var result in results)
            {
                var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
                logBuilder.Append(log);
            }

            loggingService.Info(logBuilder.ToString());
        }

        public async Task InsertLetstalk(int requestId, string broadcastSessionId, int statusId)
        {
            var command = new InsertLetstalkCommand
            {
                RequestId = requestId,
                BroadcastSessionId = broadcastSessionId,
                StatusId = statusId
            };

            await repository.ExecuteAsync(command);
        }

        public async Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId)
        {
            var command = new UpdateLetstalkCommand
            {
                RequestId = requestId,
                BroadcastSessionId = broadcastSessionId,
                StatusId = statusId
            };

            await repository.ExecuteAsync(command);
        }
    }
}