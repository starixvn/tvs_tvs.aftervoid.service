using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Caching;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Resources;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Core.Services
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

        public NotificationSignalRServiceImp(TicketVoidingSystemApi ticketVoidingSystemApi, VoidBetAutomationService voidBetAutomationService,
            ICache cacheService, LoggingService loggingService)
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

        public async Task UpdateFailedStatusForRequest(int requestId)
        {
            var failedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Failed } };
            await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(failedRequests);
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

        public async Task UpdateCompletedStatusForRequests(int requestId, int voidingHistoryId)
        {
            var requestInfo = await voidBetAutomationService.GetRequestSimpleInfo(requestId, voidingHistoryId);
            var completedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Completed, SimpleInfo = requestInfo } };
            await ticketVoidingSystemApi.SignalRUpdateRequestStatusToAllUsers(completedRequests);
        }

        private async Task NotifyVoidFailedMessage(IEnumerable<VoidBetAutomationInfo> failedRequests)
        {
            var (_, jsonCacheResult) = await cacheService.TryGetAsync<string>(FanexCacheConstant.FailedRequestNotified);
            var failedRequestNotified = jsonCacheResult == null ? new List<CacheFailedRequestNotified>()
                : JsonConvert.DeserializeObject<IList<CacheFailedRequestNotified>>(jsonCacheResult);
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

                var serializedJsonCache = JsonConvert.SerializeObject(failedRequestNotified);
                var cachePolicy = new CacheItemOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                await cacheService.SetAsync(FanexCacheConstant.FailedRequestNotified, serializedJsonCache,
                    cachePolicy);
                loggingService.Info("Total items in cache FailedRequestNotified: " + failedRequestNotified.Count());
            }
        }

        private static string GetMessageVoidRequestFailed(int requestId, int matchId)
        {
            return matchId > 0 ? $"Failed to void Request ID: {requestId} Match ID: {matchId}" : $"Failed to void Request ID: {requestId}";
        }

        public async Task NotifyVoidResultToUser(string userName, IEnumerable<Result> results)
        {
            await ticketVoidingSystemApi.SignalRNotifyVoidResultToUser(userName, results);
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