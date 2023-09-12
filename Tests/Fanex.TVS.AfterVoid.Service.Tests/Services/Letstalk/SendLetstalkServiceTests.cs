using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk;

public class SendLetstalkServiceTests
{
    private readonly SendLetstalkServiceStage letstalkServiceStage;
    private readonly LoggingService loggingService;
    private readonly SendLetstalkServiceImp service;
    private readonly SettledBetLetstalkService settledBetLetstalkService;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public SendLetstalkServiceTests()
    {
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var abnormalBetLetstalkServiceImp = Substitute.For<AbnormalBetLetstalkService>();
        settledBetLetstalkService = Substitute.For<SettledBetLetstalkService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        letstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        loggingService = Substitute.For<LoggingService>();
        service = new SendLetstalkServiceImp(voidBetAutomationService, abnormalBetLetstalkServiceImp,
            settledBetLetstalkService, notificationSignalRService
            , letstalkServiceStage, loggingService);
    }

    private static RequestDetailViewModel CreateRequestDetailViewModel(int issueTypeId)
    {
        return new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = issueTypeId },
            Messages = new List<Message>(),
            EventByMatch = new RequestEventModel()
        };
    }

    [Fact]
    public async Task Execute_HasNotAnyRequest_ReturnExpectedResults()
    {
        // Arrange
        letstalkServiceStage.GetVoidBetAutomationRunLetstalk().Returns(new List<VoidBetAutomationInfo>());

        // Act
        await service.Execute();

        // Assert
        await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
    }

    [Fact]
    public async Task Execute_HasRequest_ReturnExpectedResults()
    {
        // Arrange
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet },
            EventByMatch = new RequestEventModel()
        };
        const int requestId1 = 1;
        const int requestId2 = 2;
        var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>
        {
            new() { IsSentLetsTalk = true },
            new() { RequestId = requestId1, IsAutoVoid = true },
            new() { RequestId = requestId2, IsAutoVoid = false },
            new() { RequestId = requestId2 }
        };
        var voidDataXmlModel = new List<VoidDataXmlModel>
        {
            new()
            {
                VoidFromDate = "12/12/2022", VoidToDate = "12/12/2022", VoidFromTime = "1:12:12 AM",
                VoidToTime = "2:12:12 AM"
            },
            new()
            {
                VoidFromDate = "2/12/2022", VoidToDate = "2/12/2022", VoidFromTime = "10:12:12 AM",
                VoidToTime = "10:12:12 AM"
            }
        };
        var voidedTickets = Builder<TicketEmailModel>.CreateListOfSize(5).Build();
        letstalkServiceStage.GetVoidBetAutomationRunLetstalk().ReturnsForAnyArgs(voidBetAutomationInfoList);
        letstalkServiceStage.GetVoidedTickets(default).ReturnsForAnyArgs(voidedTickets);
        letstalkServiceStage.GetVoidDataXml(default).ReturnsForAnyArgs(voidDataXmlModel);
        voidBetAutomationService.GetRequestById(requestId1, default)
            .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
        voidBetAutomationService.GetRequestById(requestId2, default)
            .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
        voidBetAutomationService.GetMatchInfo(default).Returns(new Match { MatchId = 1 });

        // Act
        await service.Execute();

        // Assert
        await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
    }

    [Fact]
    public async Task Execute_SendLetstalkSettleDone_ReturnExpectedResults()
    {
        // Arrange
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalAccount },
            EventByMatch = new RequestEventModel()
        };
        const int requestId1 = 1;
        const int requestId2 = 2;
        var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>
        {
            new() { IsSentLetsTalk = true },
            new() { RequestId = requestId1, IsAutoVoid = true },
            new() { RequestId = requestId2, IsAutoVoid = false },
            new() { RequestId = requestId2 }
        };
        var voidDataXmlModel = new List<VoidDataXmlModel>();
        var voidedTickets = Builder<TicketEmailModel>.CreateListOfSize(5).Build();
        letstalkServiceStage.GetVoidBetAutomationRunLetstalk().ReturnsForAnyArgs(voidBetAutomationInfoList);
        letstalkServiceStage.GetVoidedTickets(default).ReturnsForAnyArgs(voidedTickets);
        letstalkServiceStage.GetVoidDataXml(default).ReturnsForAnyArgs(voidDataXmlModel);
        voidBetAutomationService.GetRequestById(requestId1, default)
            .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
        voidBetAutomationService.GetRequestById(requestId2, default)
            .Returns(CreateRequestDetailViewModel(IssueTypeConstant.AbnormalBet));
        settledBetLetstalkService.Send(default, default).ReturnsForAnyArgs(true);
        voidBetAutomationService.GetMatchInfo(default).Returns((Match)null);
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);

        // Act
        await service.Execute();

        // Assert
        await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
    }

    [Fact]
    public async Task Execute_HasException_ReturnExpectedResults()
    {
        // Arrange
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet },
            EventByMatch = new RequestEventModel()
        };
        letstalkServiceStage.GetVoidBetAutomationRunLetstalk()
            .ReturnsForAnyArgs(new List<VoidBetAutomationInfo> { new() });
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
        settledBetLetstalkService
            .When(x => x.Send(Arg.Any<NotificationLetstalkModel>(), Arg.Any<IEnumerable<TicketEmailModel>>()))
            .Do(x => x.Throws(new Exception()));
        voidBetAutomationService.GetMatchInfo(default).Returns(new Match());

        // Act
        await service.Execute();

        // Assert
        await letstalkServiceStage.Received(1).GetVoidBetAutomationRunLetstalk();
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
    }
}