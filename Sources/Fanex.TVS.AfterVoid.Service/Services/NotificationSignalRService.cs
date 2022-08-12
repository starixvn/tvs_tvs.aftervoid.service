using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Caching;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Resources;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TicketVoidingSystem.Core.SignalR;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    public interface NotificationSignalRService
    {
        Task UpdateRunningStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus);

        Task UpdateFailedStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus);

        Task UpdateFailedStatusForRequest(int requestId);

        Task UpdateCompletedStatusForRequests(int requestId, int voidingHistoryId);

        Task NotifyVoidResultToUser(string userName, IEnumerable<Result> results);

        Task NotifyErrorWhenExecuteAfterVoidServiceToUser(string userName);
    }

    public class NotificationSignalRServiceImp : NotificationSignalRService
    {
        private readonly TicketVoidingSystemApi ticketVoidingSystemApi;
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly ICache cacheService;
        private readonly LoggingService loggingService;
        private readonly IHubContext<NotificationHub, ClientEvent> hubContext;

        public NotificationSignalRServiceImp(TicketVoidingSystemApi ticketVoidingSystemApi, VoidBetAutomationService voidBetAutomationService,
            ICache cacheService, LoggingService loggingService, IHubContext<NotificationHub, ClientEvent> hubContext)
        {
            this.ticketVoidingSystemApi = ticketVoidingSystemApi;
            this.voidBetAutomationService = voidBetAutomationService;
            this.cacheService = cacheService;
            this.loggingService = loggingService;
            this.hubContext = hubContext;
        }

        public async Task UpdateRunningStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            var runningRequests = requestsWillUpdateStatus.Where(x => x.StatusId == ExecuteStatus.DBJobRunning || x.StatusId == ExecuteStatus.DBJobDone)
                .Select(x => new RequestStatus { RequestId = x.RequestId, Status = RequestStatusConstant.Running });
            if (runningRequests.Any())
            {
                await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(runningRequests);
                await hubContext.Clients.All.updateRequestStatus(runningRequests);
            }
        }

        public async Task UpdateFailedStatusForRequest(int requestId)
        {
            var failedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Failed } };
            await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(failedRequests);
            await hubContext.Clients.All.updateRequestStatus(failedRequests);
        }

        public async Task UpdateFailedStatusForRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            var failedRequests = requestsWillUpdateStatus.Where(x => x.StatusId == ExecuteStatus.DBJobFailed);

            if (failedRequests.Any())
            {
                var (_, jsonCacheResult) = await cacheService.TryGetAsync<string>(FanexCacheConstant.FailedRequestNotified);
                var failedRequestNotified = jsonCacheResult == null ? new List<CacheFailedRequestNotified>()
                    : JsonConvert.DeserializeObject<List<CacheFailedRequestNotified>>(jsonCacheResult);

                var requestNotNotify = failedRequests.Where(x =>
                    !failedRequestNotified.Any(y => y.RequestId == x.RequestId && y.VoidingHistoryId == x.VoidingHistory));

                await UpdateFailedStatusForRequestsNotNotify(requestNotNotify);
                await NotifyVoidFailedMessage(requestNotNotify);
                await UpdateCacheFailedRequestNotified(failedRequestNotified, requestNotNotify);
            }
        }

        public async Task UpdateCompletedStatusForRequests(int requestId, int voidingHistoryId)
        {
            var requestInfo = await voidBetAutomationService.GetRequestSimpleInfo(requestId, voidingHistoryId);
            var completedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Completed, SimpleInfo = requestInfo } };
            await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(completedRequests);
            await hubContext.Clients.All.updateRequestStatus(completedRequests);
        }

        private async Task UpdateFailedStatusForRequestsNotNotify(IEnumerable<VoidBetAutomationInfo> requestNotNotify)
        {
            if (requestNotNotify.Any())
            {
                var requests = requestNotNotify.Select(x => new RequestStatus
                { RequestId = x.RequestId, Status = RequestStatusConstant.Failed });
                await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(requests);
                await hubContext.Clients.All.updateRequestStatus(requests);
            }
        }

        private async Task NotifyVoidFailedMessage(IEnumerable<VoidBetAutomationInfo> requestNotNotify)
        {
            foreach (var request in requestNotNotify)
            {
                if (request.IsAutoVoid)
                {
                    continue;
                }
                var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(request.VoidingHistory);
                if (voidDataXml == null)
                {
                    continue;
                }

                var errorMessage = GetMessageVoidRequestFailed(request.RequestId, request.MatchId);
                await ticketVoidingSystemApi.SignalRNotifyErrorMessageToUser(voidDataXml.CreatorName, errorMessage);
                await hubContext.Clients.User(voidDataXml.CreatorName).showErrorMessage(errorMessage);
            }
        }

        private static string GetMessageVoidRequestFailed(int requestId, int matchId)
        {
            return matchId > 0 ? $"Failed to void Request ID: {requestId} Match ID: {matchId}" : $"Failed to void Request ID: {requestId}";
        }

        private async Task UpdateCacheFailedRequestNotified(List<CacheFailedRequestNotified> failedRequestNotified, IEnumerable<VoidBetAutomationInfo> requestNotNotify)
        {
            if (requestNotNotify.Any())
            {
                var beforeCount = failedRequestNotified.Count;
                failedRequestNotified.AddRange(requestNotNotify.Select(x => new CacheFailedRequestNotified
                {
                    RequestId = x.RequestId,
                    VoidingHistoryId = x.VoidingHistory
                }));
                var serializedJsonCache = JsonConvert.SerializeObject(failedRequestNotified);
                var cachePolicy = new CacheItemOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                await cacheService.SetAsync(FanexCacheConstant.FailedRequestNotified, serializedJsonCache,
                    cachePolicy);
                loggingService.Info($"Total items in cache FailedRequestNotified - Before: {beforeCount} - After: {failedRequestNotified.Count}");
            }
        }

        public async Task NotifyVoidResultToUser(string userName, IEnumerable<Result> results)
        {
            await ticketVoidingSystemApi.SignalRNotifyVoidResultToUser(userName, results);
            await hubContext.Clients.User(userName).showVoidResult(results);
        }

        public async Task NotifyErrorWhenExecuteAfterVoidServiceToUser(string userName)
        {
            var results = new List<Result>
            {
                new()
                {
                    ErrorCode = ErrorCode.ExceptionWhenRunAfterVoid,
                    ErrorMsg = Messages.ExceptionWhenRunAfterVoid
                }
            };

            await NotifyVoidResultToUser(userName, results);
        }
    }
}