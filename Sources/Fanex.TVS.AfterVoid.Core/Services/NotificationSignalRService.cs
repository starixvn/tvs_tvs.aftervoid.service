using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Caching;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface NotificationSignalRService
    {
        Task UpdateRunningStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus);

        Task UpdateFailedStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus);

        Task UpdateFailedStatusForRequest(int requestId);

        void NotifyException(string userName);

        Task NotifyCompletedRequests(int requestId, int voidingHistoryId);

        void NotifyVoidResult(IEnumerable<Result> results, string userName);
    }

    public class NotificationSignalRServiceImp : NotificationSignalRService
    {
        private readonly TicketVoidingSystemApi ticketVoidingSystemApi;
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly ICacheService cacheService;
        private readonly LoggingService loggingService;

        public NotificationSignalRServiceImp(TicketVoidingSystemApi ticketVoidingSystemApi, VoidBetAutomationService voidBetAutomationService,
            ICacheService cacheService, LoggingService loggingService)
        {
            this.ticketVoidingSystemApi = ticketVoidingSystemApi;
            this.voidBetAutomationService = voidBetAutomationService;
            this.cacheService = cacheService;
            this.loggingService = loggingService;
        }

        public async Task UpdateRunningStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            var runningRequests = requestsWillUpdateStatus.Where(x => x.StatusId == ExecuteStatus.DBJobRunning || x.StatusId == ExecuteStatus.DBJobDone)
                .Select(x => new RequestStatus { RequestId = x.RequestId, Status = RequestStatusConstant.Running });
            if (runningRequests.Any())
            {
                await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(runningRequests);
            }
        }

        public Task UpdateFailedStatusForRequest(int requestId)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateFailedStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            var failedRequests = requestsWillUpdateStatus.Where(x => x.StatusId == ExecuteStatus.DBJobFailed);

            if (failedRequests.Any())
            {
                var requestStatusList = failedRequests.Select(x => new RequestStatus { RequestId = x.RequestId, Status = RequestStatusConstant.Failed });
                await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(requestStatusList);
                await NotifyVoidFailedMessage(failedRequests);
            }
        }

        private async Task NotifyVoidFailedMessage(IEnumerable<VoidBetAutomationInfo> failedRequests)
        {
            var cacheResult = await cacheService.GetAsync<IList<CacheFailedRequestNotified>>(FanexCacheConstant.FailedRequestNotified);
            var failedRequestNotified = cacheResult == null ? new List<CacheFailedRequestNotified>() : cacheResult.ToList();
            foreach (var request in failedRequests)
            {
                if (request.IsAutoVoid || failedRequestNotified.Any(x =>
                    x.RequestId == request.RequestId && x.VoidingHistoryId == request.VoidingHistory))
                {
                    continue;
                }
                var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(request.VoidingHistory);
                if (voidDataXml == null)
                {
                    continue;
                }

                await ticketVoidingSystemApi.SignalRNotifyErrorMessageToUser(voidDataXml.CreatorName,
                    GetMessageVoidRequestFailed(request.RequestId, request.MatchId));

                failedRequestNotified.Add(new CacheFailedRequestNotified
                {
                    RequestId = request.RequestId,
                    VoidingHistoryId = request.VoidingHistory
                });

                var cachePolicy = new CacheItemOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                await cacheService.SetAsync(FanexCacheConstant.FailedRequestNotified, failedRequestNotified,
                    cachePolicy);
                loggingService.Info("Total items in cache FailedRequestNotified: " + failedRequestNotified.Count);
            }
        }

        private string GetMessageVoidRequestFailed(int requestId, int matchId)
        {
            if (matchId > 0)
            {
                return $"Failed to void Request ID: {requestId} Match ID: {matchId}";
            }

            return $"Failed to void Request ID: {requestId}";
        }

        public void NotifyException(string userName)
        {
            throw new NotImplementedException();
        }

        public Task NotifyCompletedRequests(int requestId, int voidingHistoryId)
        {
            throw new NotImplementedException();
        }

        public void NotifyVoidResult(IEnumerable<Result> results, string userName)
        {
            throw new NotImplementedException();
        }
    }
}