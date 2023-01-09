using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk
{
    public class SendLetstalkServiceTests
    {
        private static RequestDetailViewModel CreateRequestDetailViewModel(int issueTypeId)
        {
            return new RequestDetailViewModel()
            {
                Request = new RequestModel { IssueTypeId = issueTypeId },
                Messages = new List<Message>(),
                EventByMatch = new RequestEventModel()
            };
        }

        [Fact]
        public async Task Execute_HasNotAnyRequest_ReturnExpectedResults()
        {
            var letstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
            letstalkServiceStage.GetVoidBetAutomationRunLetstalk().ReturnsForAnyArgs(new List<VoidBetAutomationInfo>());
            var service = new SendLetstalkServiceImp(default, default, default, default, letstalkServiceStage, default);

            await service.Execute();

            await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
        }

        [Fact]
        public async Task Execute_HasRequest_ReturnExpectedResults()
        {
            const int requestId1 = 1;
            const int requestId2 = 2;
            var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>
            {
                new() { IsSentLetsTalk = true },
                new() { IsInsert = true, RequestId = requestId1, IsAutoVoid = true },
                new() { IsInsert = true, RequestId = requestId2, IsAutoVoid = false },
                new() { IsInsert = false, RequestId = requestId2 },
            };
            var voidDataXmlModel = new List<VoidDataXmlModel>
            {
                new()
                {
                    VoidFromDate = "12/12/2022", VoidToDate = "12/12/2022", VoidFromTime = "1:12:12 AM",
                    VoidToTime = "2:12:12 AM"
                }
            };
            var voidedTickets = Builder<TicketEmailModel>.CreateListOfSize(5).Build();
            var letstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var settledBetLetstalkService = Substitute.For<SettledBetLetstalkService>();
            var abnormalBetLetstalkService = Substitute.For<AbnormalBetLetstalkService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var loggingService = Substitute.For<LoggingService>();
            letstalkServiceStage.GetVoidBetAutomationRunLetstalk().ReturnsForAnyArgs(voidBetAutomationInfoList);
            letstalkServiceStage.GetVoidedTickets(default).ReturnsForAnyArgs(voidedTickets);
            letstalkServiceStage.GetVoidDataXml(default).ReturnsForAnyArgs(voidDataXmlModel);
            voidBetAutomationService.GetRequestById(requestId1, default)
                .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
            voidBetAutomationService.GetRequestById(requestId2, default)
                .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));

            voidBetAutomationService.GetMatchInfo(default).Returns(new Match());
            var service = new SendLetstalkServiceImp(voidBetAutomationService, abnormalBetLetstalkService,
                settledBetLetstalkService, notificationSignalRService, letstalkServiceStage, loggingService);

            await service.Execute();

            await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
        }

        [Fact]
        public async Task Execute_SendLetstalkSettleDone_ReturnExpectedResults()
        {
            const int requestId1 = 1;
            const int requestId2 = 2;
            var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>
            {
                new() { IsSentLetsTalk = true },
                new() { IsInsert = true, RequestId = requestId1, IsAutoVoid = true },
                new() { IsInsert = true, RequestId = requestId2, IsAutoVoid = false },
                new() { IsInsert = false, RequestId = requestId2 },
            };
            var voidDataXmlModel = new List<VoidDataXmlModel>
            {
                new()
                {
                    VoidFromDate = "12/12/2022", VoidToDate = "12/12/2022", VoidFromTime = "1:12:12 AM",
                    VoidToTime = "2:12:12 AM"
                }
            };
            var voidedTickets = Builder<TicketEmailModel>.CreateListOfSize(5).Build();
            var letstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var settledBetLetstalkService = Substitute.For<SettledBetLetstalkService>();
            var abnormalBetLetstalkService = Substitute.For<AbnormalBetLetstalkService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var loggingService = Substitute.For<LoggingService>();
            letstalkServiceStage.GetVoidBetAutomationRunLetstalk().ReturnsForAnyArgs(voidBetAutomationInfoList);
            letstalkServiceStage.GetVoidedTickets(default).ReturnsForAnyArgs(voidedTickets);
            letstalkServiceStage.GetVoidDataXml(default).ReturnsForAnyArgs(voidDataXmlModel);
            voidBetAutomationService.GetRequestById(requestId1, default)
                .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
            voidBetAutomationService.GetRequestById(requestId2, default)
                .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
            settledBetLetstalkService.Send(default, default).ReturnsForAnyArgs(true);
            voidBetAutomationService.GetMatchInfo(default).Returns(new Match());
            var service = new SendLetstalkServiceImp(voidBetAutomationService, abnormalBetLetstalkService,
                settledBetLetstalkService, notificationSignalRService, letstalkServiceStage, loggingService);

            await service.Execute();

            await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
        }

        [Fact]
        public async Task Execute_HasException_ReturnExpectedResults()
        {
            var letstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var settledBetLetstalkService = Substitute.For<SettledBetLetstalkService>();
            var abnormalBetLetstalkService = Substitute.For<AbnormalBetLetstalkService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var loggingService = Substitute.For<LoggingService>();
            letstalkServiceStage.GetVoidBetAutomationRunLetstalk()
                .ReturnsForAnyArgs(new List<VoidBetAutomationInfo> { new() });

            settledBetLetstalkService
                .When(x => x.Send(Arg.Any<NotificationLetstalkModel>(), Arg.Any<IEnumerable<TicketEmailModel>>()))
                .Do(x => x.Throws(new Exception()));
            voidBetAutomationService.GetMatchInfo(default).Returns(new Match());
            var service = new SendLetstalkServiceImp(voidBetAutomationService, abnormalBetLetstalkService,
                settledBetLetstalkService, notificationSignalRService, letstalkServiceStage, loggingService);

            await service.Execute();

            await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
            loggingService.Error(Arg.Any<string>());
        }
    }
}