using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.Main.Tests
{
    public class AfterVoidServiceImpTests
    {
        public static IEnumerable<object[]> AutomationData =>
            new List<object[]>
            {
                new object[] { new AutomationStatistic
                {   RequestsWillUpdateStatus = new List<VoidBetAutomationInfo>(),
                    RequestsWillRunAfterVoid = null
                }},
                new object[] { new AutomationStatistic
                {   RequestsWillUpdateStatus = new List<VoidBetAutomationInfo>(),
                    RequestsWillRunAfterVoid = new List<VoidBetAutomationInfo>()
                }}
            };

        [Theory]
        [MemberData(nameof(AutomationData))]
        public async Task Execute_ShouldNotRunAnyVoidService(AutomationStatistic automationStatistic)
        {
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidBetAutomationStatistic().ReturnsForAnyArgs(automationStatistic);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, default, default,
                notificationSignalRService);

            await service.Execute();

            await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetRequestById(default, default);
            await notificationSignalRService.Received(1).UpdateRunningStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, automationStatistic.RequestsWillUpdateStatus)));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, automationStatistic.RequestsWillUpdateStatus)));
        }

        [Fact]
        public async Task Execute_ShouldCallAutoVoidService()
        {
            var fakeAutomationStatistic = new AutomationStatistic()
            {
                RequestsWillUpdateStatus = Builder<VoidBetAutomationInfo>.CreateListOfSize(3).Build(),
                RequestsWillRunAfterVoid = Builder<VoidBetAutomationInfo>.CreateListOfSize(3).All().With(x => x.IsAutoVoid = true).Build()
            };
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidBetAutomationStatistic().ReturnsForAnyArgs(fakeAutomationStatistic);
            var requestAutoVoidService = Substitute.For<RequestAutoVoidService>();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new AfterVoidServiceImp(voidBetAutomationService, requestAutoVoidService, default, default, loggingService,
                notificationSignalRService);

            await service.Execute();

            await notificationSignalRService.Received(1).UpdateRunningStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await voidBetAutomationService.Received(3).GetRequestById(
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.RequestId == x)),
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.MatchId == x)));
            await requestAutoVoidService.Received(1).Execute(Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                Equals(x, fakeAutomationStatistic.RequestsWillRunAfterVoid)));
            loggingService.ReceivedWithAnyArgs(1).Info(default);
        }

        [Fact]
        public async Task Execute_ShouldCallVoid14Service()
        {
            var requestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.ArchivedTicket } };
            var fakeAutomationStatistic = new AutomationStatistic()
            {
                RequestsWillUpdateStatus = Builder<VoidBetAutomationInfo>.CreateListOfSize(3).Build(),
                RequestsWillRunAfterVoid = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                    .All().With(x => x.IsAutoVoid = false).Build()
            };
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidBetAutomationStatistic().ReturnsForAnyArgs(fakeAutomationStatistic);
            voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
            var requestVoid14Service = Substitute.For<RequestVoid14Service>();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, requestVoid14Service, loggingService,
                notificationSignalRService);

            await service.Execute();

            await notificationSignalRService.Received(1).UpdateRunningStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await voidBetAutomationService.Received(3).GetRequestById(
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.RequestId == x)),
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.MatchId == x)));
            await requestVoid14Service.Received(1).Execute(Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                Equals(x, fakeAutomationStatistic.RequestsWillRunAfterVoid)));
            loggingService.ReceivedWithAnyArgs(1).Info(default);
        }

        [Fact]
        public async Task Execute_ShouldCallManualVoidService()
        {
            var requestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } };
            var fakeAutomationStatistic = new AutomationStatistic()
            {
                RequestsWillUpdateStatus = Builder<VoidBetAutomationInfo>.CreateListOfSize(3).Build(),
                RequestsWillRunAfterVoid = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                    .All().With(x => x.IsAutoVoid = false).Build()
            };
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidBetAutomationStatistic().ReturnsForAnyArgs(fakeAutomationStatistic);
            voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
            var requestManualVoidService = Substitute.For<RequestManualVoidService>();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new AfterVoidServiceImp(voidBetAutomationService, default, requestManualVoidService, default, loggingService,
                notificationSignalRService);

            await service.Execute();

            await notificationSignalRService.Received(1).UpdateRunningStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequests(
                Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                    Equals(x, fakeAutomationStatistic.RequestsWillUpdateStatus)));
            await voidBetAutomationService.Received(3).GetRequestById(
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.RequestId == x)),
                Arg.Is<int>(x => fakeAutomationStatistic.RequestsWillRunAfterVoid.Any(y => y.MatchId == x)));
            await requestManualVoidService.Received(1).Execute(Arg.Is<IEnumerable<VoidBetAutomationInfo>>(x =>
                Equals(x, fakeAutomationStatistic.RequestsWillRunAfterVoid)));
            loggingService.ReceivedWithAnyArgs(1).Info(default);
        }
    }
}