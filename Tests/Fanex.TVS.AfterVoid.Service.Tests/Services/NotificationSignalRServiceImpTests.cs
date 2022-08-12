using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Caching;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TicketVoidingSystem.Core.SignalR;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.Tests
{
    public class NotificationSignalRServiceImpTests
    {
        [Fact()]
        public async Task UpdateRunningStatusForRequests_ShouldNotCallUpdateApi()
        {
            var fakeRequestsWillUpdateStatus = new List<VoidBetAutomationInfo>();
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, default, default, default, hubContext);

            await service.UpdateRunningStatusForRequests(fakeRequestsWillUpdateStatus);

            await ticketVoidingSystemApi.DidNotReceive()
                 .SignalRUpdateRequestStatusToAllUsers(Arg.Any<IEnumerable<RequestStatus>>());
            await hubContext.Clients.All.DidNotReceiveWithAnyArgs().updateRequestStatus(Arg.Any<IEnumerable<RequestStatus>>());
        }

        [Fact()]
        public async Task UpdateRunningStatusForRequests_ShouldCallUpdateApi()
        {
            var fakeRequestsWillUpdateStatus = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .Random(1).With(x => x.StatusId = ExecuteStatus.DBJobRunning)
                .Random(1).With(x => x.StatusId = ExecuteStatus.DBJobDone).Build();
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, default, default, default, hubContext);

            await service.UpdateRunningStatusForRequests(fakeRequestsWillUpdateStatus);

            await ticketVoidingSystemApi.Received(1)
                .SignalRUpdateRequestStatusToAllUsers(Arg.Any<IEnumerable<RequestStatus>>());
            await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Any<IEnumerable<RequestStatus>>());
        }

        [Fact()]
        public async Task UpdateFailedStatusForRequest_ShouldCallUpdateApi()
        {
            var fakeRequestId = 113;
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, default, default, default, hubContext);

            await service.UpdateFailedStatusForRequest(fakeRequestId);

            await ticketVoidingSystemApi.Received(1)
                .SignalRUpdateRequestStatusToAllUsers(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                   && x.Any(y => y.RequestId == fakeRequestId)));
            await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                && x.Any(y => y.RequestId == fakeRequestId)));
        }

        [Fact()]
        public async Task UpdateFailedStatusForRequests_ShouldNotCallCacheService()
        {
            var fakeRequestsWillUpdateStatus = new List<VoidBetAutomationInfo>();
            var cacheService = Substitute.For<ICache>();
            var service = new NotificationSignalRServiceImp(default, default, cacheService, default, default);

            await service.UpdateFailedStatusForRequests(fakeRequestsWillUpdateStatus);

            await cacheService.DidNotReceive()
                .TryGetAsync<string>(Arg.Is<string>(x => x == FanexCacheConstant.FailedRequestNotified));
        }

        [Fact()]
        public async Task UpdateFailedStatusForRequests_ShouldCallUpdateApi()
        {
            var fakeFailRequestId = 331;
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeRequestsWillUpdateStatus = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.DBJobRunning)
                .Random(1).With(x => x.StatusId = ExecuteStatus.DBJobFailed)
                .And(x => x.RequestId = fakeFailRequestId).And(x => x.IsAutoVoid = false).Build();
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var cacheService = Substitute.For<ICache>();
            cacheService.TryGetAsync<string>(Arg.Is<string>(x => x == FanexCacheConstant.FailedRequestNotified))
                .Returns((false, null));
            var loggingService = Substitute.For<LoggingService>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, voidBetAutomationService, cacheService, loggingService, hubContext);

            await service.UpdateFailedStatusForRequests(fakeRequestsWillUpdateStatus);

            // Assert
            await cacheService.Received(1)
                .TryGetAsync<string>(Arg.Is<string>(x => x == FanexCacheConstant.FailedRequestNotified));
            await ticketVoidingSystemApi.Received(1)
                .SignalRUpdateRequestStatusToAllUsers(Arg.Any<IEnumerable<RequestStatus>>());
            await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Any<IEnumerable<RequestStatus>>());
            await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(Arg.Any<int>());
            await ticketVoidingSystemApi.Received(1)
                .SignalRNotifyErrorMessageToUser(Arg.Is<string>(x => x == fakeVoidDataXml.CreatorName), Arg.Any<string>());
            await hubContext.Clients.User(fakeVoidDataXml.CreatorName).Received(1).showErrorMessage(Arg.Any<string>());
            await cacheService.Received(1)
                .SetAsync(Arg.Is<string>(x => x == FanexCacheConstant.FailedRequestNotified), Arg.Any<string>(), Arg.Any<CacheItemOptions>());
            loggingService.Received(1).Info(Arg.Any<string>());
        }

        [Fact()]
        public async Task UpdateCompletedStatusForRequests_ShouldCallUpdateApi()
        {
            var fakeRequestId = 113;
            var fakeVoidingHistoryId = 223;
            var fakeRequestSimple = Builder<RequestSimpleInfo>.CreateNew().Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetRequestSimpleInfo(default, default).ReturnsForAnyArgs(fakeRequestSimple);
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, voidBetAutomationService, default, default, hubContext);

            await service.UpdateCompletedStatusForRequests(fakeRequestId, fakeVoidingHistoryId);

            await voidBetAutomationService.Received(1)
                .GetRequestSimpleInfo(Arg.Is<int>(x => x == fakeRequestId), Arg.Is<int>(x => x == fakeVoidingHistoryId));
            await ticketVoidingSystemApi.Received(1)
                .SignalRUpdateRequestStatusToAllUsers(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                    && x.Any(y => y.RequestId == fakeRequestId)));

            await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                && x.Any(y => y.RequestId == fakeRequestId)));
        }

        [Fact()]
        public async Task NotifyVoidResultToUser_ShouldCallUpdateApi()
        {
            var fakeUsername = "aaaxxx";
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, default, default, default, hubContext);

            await service.NotifyVoidResultToUser(fakeUsername, fakeResults);

            await ticketVoidingSystemApi.Received(1)
                .SignalRNotifyVoidResultToUser(Arg.Is<string>(x => x == fakeUsername), Arg.Is<IEnumerable<Result>>(x => x == fakeResults));
            await hubContext.Clients.User(fakeUsername).Received(1)
                .showVoidResult(Arg.Is<IEnumerable<Result>>(x => Equals(x, fakeResults)));
        }

        [Fact()]
        public async Task NotifyErrorWhenExecuteAfterVoidServiceToUser_ShouldCallUpdateApi()
        {
            var fakeUsername = "aaaxxx";
            var ticketVoidingSystemApi = Substitute.For<TicketVoidingSystemApi>();
            var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
            var service = new NotificationSignalRServiceImp(ticketVoidingSystemApi, default, default, default, hubContext);

            await service.NotifyErrorWhenExecuteAfterVoidServiceToUser(fakeUsername);

            await ticketVoidingSystemApi.Received(1)
                .SignalRNotifyVoidResultToUser(Arg.Is<string>(x => x == fakeUsername),
                    Arg.Is<IEnumerable<Result>>(x => x.Count() == 1 && x.Any(y => y.ErrorCode == ErrorCode.ExceptionWhenRunAfterVoid)));
            await hubContext.Clients.User(fakeUsername).Received(1)
                .showVoidResult(Arg.Is<IEnumerable<Result>>(x => x.Count() == 1 && x.Any(y => y.ErrorCode == ErrorCode.ExceptionWhenRunAfterVoid)));
        }
    }
}