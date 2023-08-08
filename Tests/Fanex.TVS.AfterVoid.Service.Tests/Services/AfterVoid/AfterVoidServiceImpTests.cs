using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class AfterVoidServiceImpTests
{
    [Fact]
    public async Task Execute_ShouldNotRunAnyVoidService()
    {
        VoidBetAutomationInfo automationStatistic = null;
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid().ReturnsForAnyArgs((automationStatistic, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(new RequestDetailViewModel());
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, default, default);

        await service.Execute();

        await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetRequestById(default, default);
    }

    [Fact]
    public async Task Execute_ShouldCallAutoVoidService()
    {
        var fakeAutomationStatistic = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsAutoVoid = true).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid().ReturnsForAnyArgs((fakeAutomationStatistic, false, "", new List<EmailResultModel>()));
        var requestAutoVoidService = Substitute.For<RequestAutoVoidService>();
        var loggingService = Substitute.For<LoggingService>();
        var service = new AfterVoidServiceImp(voidBetAutomationService, requestAutoVoidService, default, default, loggingService);

        await service.Execute();

        await requestAutoVoidService.Received(1).Execute(Arg.Is<VoidBetAutomationInfo>(x =>
            Equals(x, fakeAutomationStatistic)), false, "");
    }

    [Fact]
    public async Task Execute_ShouldCallVoid14Service()
    {
        var requestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = IssueTypeConstant.ArchivedTicket } };
        var fakeAutomationStatistic = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsAutoVoid = false).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid().ReturnsForAnyArgs((fakeAutomationStatistic, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
        var requestVoid14Service = Substitute.For<RequestVoid14Service>();
        var loggingService = Substitute.For<LoggingService>();
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, default, requestVoid14Service,
            loggingService);

        await service.Execute();

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
        var requestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = issueTypeId } };
        var fakeAutomationStatistic = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsAutoVoid = false).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidBetAutomationRunAfterVoid().ReturnsForAnyArgs((fakeAutomationStatistic, false, "", new List<EmailResultModel>()));
        voidBetAutomationService.GetRequestById(default, default).ReturnsForAnyArgs(requestInfo);
        var requestManualVoidService = Substitute.For<RequestManualVoidService>();
        var loggingService = Substitute.For<LoggingService>();
        var service = new AfterVoidServiceImp(voidBetAutomationService, default, requestManualVoidService, default, loggingService);

        await service.Execute();

        await requestManualVoidService.Received(1).Execute(Arg.Is<VoidBetAutomationInfo>(x =>
            Equals(x, fakeAutomationStatistic)), false, "", Arg.Any<IEnumerable<EmailResultModel>>());
        loggingService.ReceivedWithAnyArgs(1).Info(default);
    }
}