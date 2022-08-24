using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    public interface VoidBetAutomationService
    {
        Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationStatistic();

        Task<RequestDetailViewModel> GetRequestById(int requestId, int matchId);

        Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int voidingHistoryId);

        Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId);

        Task UpdateServiceStatusBySession(int sessionId, int statusId);

        Task UpdateMessageIdByVoidingHistory(int requestId, int voidingHistoryId, int messageId);

        Task UpdateMessageIdBySession(int sessionId, int messageId);

        Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

        Task UpdateAllAfterVoidSteps(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory);

        Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content);

        Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsForSendEmail(int voidingHistoryId);

        Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId);

        Task<Match> GetMatchInfo(int matchId);

        Task UpdateTicketVoidingHistory(int voidingHistoryId, int? statusId, int? reasonId, int? messageId,
            string transIds, DateTime? minWinLostDate, DateTime? maxWinLostDate);
    }

    public class VoidBetAutomationServiceImp : VoidBetAutomationService
    {
        private readonly IDynamicRepository repository;

        public VoidBetAutomationServiceImp(IDynamicRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationStatistic()
        {
            var requestsRunAfterVoid = await repository.FetchAsync<VoidBetAutomationInfo>(new GetAllVoidBetAutomationInfoCriteria());
            return requestsRunAfterVoid;
        }

        public async Task<RequestDetailViewModel> GetRequestById(int requestId, int matchId)
        {
            var criteria = new SelectRequestCriteria
            {
                RequestId = requestId
            };
            var (requests, requestEvents, _) = await repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(criteria);

            var firstRequest = requests.FirstOrDefault();
            var viewModel = new RequestDetailViewModel
            {
                Request = firstRequest,
                EventByMatch = IsIssueTypeHasMatchId(firstRequest.IssueTypeId) ? requestEvents.FirstOrDefault(x => x.MatchId == matchId)
                    : requestEvents.FirstOrDefault()
            };

            return viewModel;
        }

        private static bool IsIssueTypeHasMatchId(int issueTypeId)
        {
            var isWrongHdpGroup = issueTypeId is IssueTypeConstant.WrongHDP or IssueTypeConstant.Wrong1stHDP;
            var isWrongOuGroup = issueTypeId is IssueTypeConstant.WrongOU or IssueTypeConstant.Wrong1stOU;
            return issueTypeId is IssueTypeConstant.DangerAccepted or IssueTypeConstant.RedCard
                   || isWrongHdpGroup || isWrongOuGroup;
        }

        public async Task<RequestSimpleInfo> GetRequestSimpleInfo(int requestId, int voidingHistoryId)
        {
            var criteria = new GetRequestSimpleInfoCriteria
            {
                RequestId = requestId,
                VoidingHistoryId = voidingHistoryId
            };
            var request = await repository.GetAsync<RequestSimpleInfo>(criteria);

            return request;
        }

        public async Task UpdateServiceStatusByVoidingHistory(int requestId, int voidingHistoryId, int statusId)
        {
            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateStatusByVoidingHistory, null, requestId, voidingHistoryId, statusId, null);
        }

        public async Task UpdateServiceStatusBySession(int sessionId, int statusId)
        {
            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateStatusBySession, sessionId, null, null, statusId, null);
        }

        public async Task UpdateMessageIdByVoidingHistory(int requestId, int voidingHistoryId, int messageId)
        {
            var afterVoidHistory = new VoidBetAutomationInfo
            {
                MessageId = messageId
            };

            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateMessageIdByVoidingHistory, null, requestId, voidingHistoryId, null, afterVoidHistory);
        }

        public async Task UpdateMessageIdBySession(int sessionId, int messageId)
        {
            var afterVoidHistory = new VoidBetAutomationInfo
            {
                MessageId = messageId
            };

            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateMessageIdBySession, sessionId, null, null, null, afterVoidHistory);
        }

        public async Task UpdateAfterVoidStep(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory)
        {
            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep, null, requestId, voidingHistoryId, null, afterVoidHistory);
        }

        public async Task UpdateAllAfterVoidSteps(int requestId, int voidingHistoryId, VoidBetAutomationInfo afterVoidHistory)
        {
            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateAllAfterVoidSteps, null, requestId, voidingHistoryId, null, afterVoidHistory);
        }

        public async Task UpdateEmailResultByVoidingHistory(int requestId, int voidingHistoryId, string content)
        {
            var afterVoidHistory = new VoidBetAutomationInfo
            {
                VoidResults = content
            };

            await UpdateVoidBetAutomationServiceInfo(VoidBetAutomationServiceUpdateType.UpdateEmailsResultByVoidingHistory, null, requestId, voidingHistoryId, null, afterVoidHistory);
        }

        private async Task UpdateVoidBetAutomationServiceInfo(int updateType, int? sessionId, int? requestId, int? voidingHistoryId, int? statusId, VoidBetAutomationInfo afterVoidHistory)
        {
            var criteria = new UpdateVoidBetAutomationInfoCriteria
            {
                UpdateType = updateType,
                SessionId = sessionId,
                RequestId = requestId,
                VoidingHistoryId = voidingHistoryId,
                StatusId = statusId
            };

            if (afterVoidHistory != null)
            {
                criteria.IsRunExternalService = afterVoidHistory.IsRunExternalService;
                criteria.MessageId = afterVoidHistory.MessageId;
                criteria.ReasonId = afterVoidHistory.ReasonId;
                criteria.IsSentBatchMessage = afterVoidHistory.IsSentBatchMessage;
                criteria.IsSentMessageToAdminSite = afterVoidHistory.IsSentMessageToAdminSite;
                criteria.IsInsertedVoidedTransaction = afterVoidHistory.IsInsertedVoidedTransaction;
                criteria.IsUpdatedVoidingHistory = afterVoidHistory.IsUpdatedVoidingHistory;
                criteria.IsSentEmail = afterVoidHistory.IsSentEmail;
                criteria.IsSentLetsTalk = afterVoidHistory.IsSentLetsTalk;
                criteria.VoidResults = afterVoidHistory.VoidResults;
            }

            await repository.ExecuteAsync(criteria);
        }

        public async Task<IEnumerable<TicketEmailModel>> GetVoidedTicketsForSendEmail(int voidingHistoryId)
        {
            var checkITUser = await CheckRequestOfITUser(voidingHistoryId) ?? new CheckRequestOfITUserModel();
            var ticketsInAllSiteDb = new List<TicketEmailModel>();
            foreach (var server in SiteDbGroupConstant.ServerList)
            {
                var criteria = new GetTicketsEmailCriteria
                {
                    SiteDbGroupId = server.Id,
                    VoidingHistoryId = voidingHistoryId,
                    IsITSupport = checkITUser.IsITSupport,
                    IsITSupportCreatedRequest = checkITUser.IsITSupportCreatedRequest
                };

                var tickets = await repository.FetchAsync<TicketEmailModel>(criteria);
                ticketsInAllSiteDb.AddRange(tickets);
            }

            return ticketsInAllSiteDb;
        }

        private async Task<CheckRequestOfITUserModel> CheckRequestOfITUser(int voidingHistoryId)
        {
            var criteria = new CheckRequestOfITUserCriteria
            {
                VoidingHistoryId = voidingHistoryId
            };

            var result = await repository.GetAsync<CheckRequestOfITUserModel>(criteria);
            return result;
        }

        public async Task<VoidDataXmlModel> GetVoidDataFromVoidingHistory(int voidingHistoryId)
        {
            var criteria = new GetVoidingHistoryCriteria
            {
                VoidingHistoryId = voidingHistoryId
            };

            return await repository.GetAsync<VoidDataXmlModel>(criteria);
        }

        public async Task<Match> GetMatchInfo(int matchId)
        {
            return await repository.GetAsync<Match>(new GetMatchInfoCriteria { MatchId = matchId, IsMatch3rd = false });
        }

        public async Task UpdateTicketVoidingHistory(int voidingHistoryId, int? statusId, int? reasonId, int? messageId,
            string transIds, DateTime? minWinLostDate, DateTime? maxWinLostDate)
        {
            var command = new UpdateHistoryCommand
            {
                VoidingHistoryId = voidingHistoryId,
                StatusId = statusId,
                ReasonId = reasonId,
                MessageId = messageId,
                TransIds = transIds,
                MinWinLostDate = minWinLostDate,
                MaxWinLostDate = maxWinLostDate
            };

            await repository.ExecuteAsync(command);
        }
    }
}