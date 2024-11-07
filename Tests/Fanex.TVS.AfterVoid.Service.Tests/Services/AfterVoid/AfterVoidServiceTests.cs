using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoRecover;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class AfterVoidServiceTests
{
    private LoggingService loggingService;
    private RequestAutoVoidService requestAutoVoidService;
    private RequestManualVoidService requestManualVoidService;
    private RequestVoid14Service requestVoid14Service;
    private VoidBetAutomationService voidBetAutomationService;
    private RequestAutoRecoverService requestAutoRecoverService;

    internal void InitFakeServiceTests()
    {
        requestManualVoidService = Substitute.For<RequestManualVoidService>();
        loggingService = Substitute.For<LoggingService>();
        requestAutoVoidService = Substitute.For<RequestAutoVoidService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        requestVoid14Service = Substitute.For<RequestVoid14Service>();
        requestAutoRecoverService = Substitute.For<RequestAutoRecoverService>();
    }

    [Fact]
    public async Task Execute_ShouldNotRunAnyVoidService()
    {
        // Arrange
        InitFakeServiceTests();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid()
            .ReturnsForAnyArgs((null, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestViewModelById(default, default).ReturnsForAnyArgs(new RequestDetailViewModel());
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, default, default, default);

        // Act
        await service.Execute();

        // Assert
        await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetRequestViewModelById(default, default);
    }

    [Fact]
    public async Task Execute_ShouldCallAutoVoidService()
    {
        // Arrange
        InitFakeServiceTests();
        var requestInfo = new RequestDetailViewModel { Request = new RequestModel { ActionId = VoidActionTypeConstant.AutoVoidInstantId } };
        var automationInfo = new VoidBetAutomationInfo { RequestInfo = requestInfo, MatchId = 1, IsAutoVoid = true };
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid()
            .ReturnsForAnyArgs((automationInfo, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestViewModelById(automationInfo.RequestId, automationInfo.MatchId)
            .ReturnsForAnyArgs(requestInfo);
        var service = new AfterVoidServiceImp(voidBetAutomationService, requestAutoVoidService, default, default, default,
            loggingService);

        // Act
        await service.Execute();

        // Assert
        await loggingService.Received(1).InfoAsync(Arg.Any<string>());
        await requestAutoVoidService.Received(1).Received().Execute(automationInfo, false, "");
    }

    [Fact]
    public async Task Execute_ShouldCallAutoRecoverService()
    {
        // Arrange
        InitFakeServiceTests();
        var requestInfo = new RequestDetailViewModel { Request = new RequestModel { ActionId = VoidActionTypeConstant.AutoRecoverInstant } };
        var automationInfo = new VoidBetAutomationInfo { RequestInfo = requestInfo, MatchId = 1, IsAutoVoid = true };
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid()
            .ReturnsForAnyArgs((automationInfo, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestViewModelById(automationInfo.RequestId, automationInfo.MatchId)
            .ReturnsForAnyArgs(requestInfo);
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, default, requestAutoRecoverService,
            loggingService);

        // Act
        await service.Execute();

        // Assert
        await loggingService.Received(1).InfoAsync(Arg.Any<string>());
        await requestAutoRecoverService.Received(1).Received().Execute(automationInfo, false, "");
    }

    [Fact]
    public async Task Execute_ShouldCallVoid14Service()
    {
        // Arrange
        InitFakeServiceTests();
        var requestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = IssueTypeConstant.ArchivedTicket } };
        var fakeAutomationStatistic =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsAutoVoid = false).Build();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid()
            .ReturnsForAnyArgs((fakeAutomationStatistic, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestViewModelById(default, default).ReturnsForAnyArgs(requestInfo);
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, requestVoid14Service, default,
            loggingService);

        // Act
        await service.Execute();

        // Assert
        await requestVoid14Service.Received(1).Execute(Arg.Is<VoidBetAutomationInfo>(x =>
            Equals(x, fakeAutomationStatistic)), false, "");
    }

    [Theory]
    [InlineData(IssueTypeConstant.DangerAccepted)]
    [InlineData(IssueTypeConstant.RedCard)]
    [InlineData(IssueTypeConstant.WrongHDP)]
    [InlineData(IssueTypeConstant.Wrong1stHDP)]
    [InlineData(IssueTypeConstant.WrongOU)]
    [InlineData(IssueTypeConstant.Wrong1stOU)]
    public async Task Execute_ShouldCallManualVoidService(int issueTypeId)
    {
        // Arrange
        InitFakeServiceTests();
        var requestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = issueTypeId } };
        var fakeAutomationStatistic =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsAutoVoid = false).Build();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid()
            .ReturnsForAnyArgs((fakeAutomationStatistic, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestViewModelById(default, default).ReturnsForAnyArgs(requestInfo);
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, requestManualVoidService, default, default,
            loggingService);

        // Act
        await service.Execute();

        // Assert
        await requestManualVoidService.Received(1).Execute(Arg.Is<VoidBetAutomationInfo>(x =>
            Equals(x, fakeAutomationStatistic)), false, "", Arg.Any<IEnumerable<EmailResultModel>>());
        await loggingService.ReceivedWithAnyArgs(1).InfoAsync(default);
    }
}