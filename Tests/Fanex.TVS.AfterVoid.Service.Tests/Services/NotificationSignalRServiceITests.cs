using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Services;
using FizzWare.NBuilder;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class NotificationSignalRServiceITests
{
    [Fact]
    public async Task UpdateFailedStatusForRequest_ShouldCallUpdateApi()
    {
        var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
        var service = new NotificationSignalRServiceImp(ticketVoidingSystemHttpClientService, default, hubContext);

        await service.UpdateFailedStatusForRequest(1);

        await ticketVoidingSystemHttpClientService.Received(1)
            .SignalRUpdateRequestStatusToAllUsers(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                && x.Any(y => y.RequestId == 1)));
        await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Is<IEnumerable<RequestStatus>>(x =>
            x.Count() == 1
            && x.Any(y => y.RequestId == 1)));
    }

    [Fact]
    public async Task UpdateCompletedStatusForRequests_ShouldCallUpdateApi()
    {
        var fakeRequestSimple = Builder<RequestSimpleInfo>.CreateNew().Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetRequestSimpleInfo(default, default).ReturnsForAnyArgs(fakeRequestSimple);
        var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
        var service = new NotificationSignalRServiceImp(ticketVoidingSystemHttpClientService, voidBetAutomationService,
            hubContext);

        await service.UpdateCompletedStatusForRequests(1, 1);

        await voidBetAutomationService.Received(1)
            .GetRequestSimpleInfo(Arg.Is<int>(x => x == 1), Arg.Is<int>(x => x == 1));
        await ticketVoidingSystemHttpClientService.Received(1)
            .SignalRUpdateRequestStatusToAllUsers(Arg.Is<IEnumerable<RequestStatus>>(x => x.Count() == 1
                && x.Any(y => y.RequestId == 1)));
        await hubContext.Clients.All.Received(1).updateRequestStatus(Arg.Is<IEnumerable<RequestStatus>>(x =>
            x.Count() == 1
            && x.Any(y => y.RequestId == 1)));
    }

    [Fact]
    public async Task NotifyVoidResultToUser_ShouldCallUpdateApi()
    {
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
        var service = new NotificationSignalRServiceImp(ticketVoidingSystemHttpClientService, default, hubContext);

        await service.NotifyVoidResultToUser("SA", fakeResults);

        await ticketVoidingSystemHttpClientService.Received(1)
            .SignalRNotifyVoidResultToUser(Arg.Is<string>(x => x == "SA"),
                Arg.Is<IEnumerable<Result>>(x => x == fakeResults));
        await hubContext.Clients.User("SA").Received(1)
            .showVoidResult(Arg.Is<IEnumerable<Result>>(x => Equals(x, fakeResults)));
    }

    [Fact]
    public async Task NotifyErrorWhenExecuteAfterVoidServiceToUser_ShouldCallUpdateApi()
    {
        const string fakeUsername = "SA";
        var ticketVoidingSystemHttpClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        var hubContext = Substitute.For<IHubContext<NotificationHub, ClientEvent>>();
        var service = new NotificationSignalRServiceImp(ticketVoidingSystemHttpClientService, default, hubContext);

        await service.NotifyErrorWhenExecuteAfterVoidServiceToUser(fakeUsername);

        await ticketVoidingSystemHttpClientService.Received(1)
            .SignalRNotifyVoidResultToUser(Arg.Is<string>(x => x == fakeUsername),
                Arg.Is<IEnumerable<Result>>(x =>
                    x.Count() == 1 && x.Any(y => y.ErrorCode == ErrorCode.ExceptionWhenRunAfterVoid)));
        await hubContext.Clients.User(fakeUsername).Received(1)
            .showVoidResult(Arg.Is<IEnumerable<Result>>(x =>
                x.Count() == 1 && x.Any(y => y.ErrorCode == ErrorCode.ExceptionWhenRunAfterVoid)));
    }
}