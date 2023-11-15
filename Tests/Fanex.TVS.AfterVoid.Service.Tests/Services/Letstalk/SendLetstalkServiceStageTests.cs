using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.Letstalk;

public class SendLetstalkServiceStageTests
{
    private LoggingService loggingService;
    private IDynamicRepository repository;
    private TVSNewSiteHttpClientService tvsNewSiteHttpClientService;
    private VoidBetAutomationService voidBetAutomationService;

    internal void InitFakeServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        loggingService = Substitute.For<LoggingService>();
        tvsNewSiteHttpClientService = Substitute.For<TVSNewSiteHttpClientService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
    }

    [Fact]
    public async Task GetVoidBetAutomationRunLetstalk_ReturnExpectedResults()
    {
        // Arrange
        InitFakeServiceTests();
        var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>();
        repository.FetchAsync<VoidBetAutomationInfo>(Arg.Any<GetAutomationInfoLetstalkCriteria>())
            .ReturnsForAnyArgs(voidBetAutomationInfoList);
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        // Act
        var actual = await service.GetVoidBetAutomationRunLetstalk();

        // Assert
        await repository.Received(1).FetchAsync<VoidBetAutomationInfo>(Arg.Any<GetAutomationInfoLetstalkCriteria>());
        AssertExtension.Equal(voidBetAutomationInfoList, actual);
    }

    [Fact]
    public async Task GetContentExcelVoidedTicket_HasSentEmail_ReturnExpectedResults()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeResult = new byte[] { 1, 2 };
        tvsNewSiteHttpClientService.GetExcelVoidedTicketForSendLetstalk(1, 1, 1, "1")
            .ReturnsForAnyArgs((true, fakeResult));
        var service = new SendLetstalkServiceStageImp(default, default, default, tvsNewSiteHttpClientService);

        // Act
        var actual = await service.GetContentExcelVoidedTicket(1, 1, 1, "1");

        // Assert
        await tvsNewSiteHttpClientService.Received(1).GetExcelVoidedTicketForSendLetstalk(1, 1, 1, "1");
        AssertExtension.Equal(fakeResult, actual);
    }

    [Fact]
    public async Task GetContentExcelVoidedTicket_ThrowException()
    {
        // Arrange
        InitFakeServiceTests();
        tvsNewSiteHttpClientService.GetExcelVoidedTicketForSendLetstalk(1, 1, 1, "1").ReturnsForAnyArgs((false, null));
        var service = new SendLetstalkServiceStageImp(default, default, default, tvsNewSiteHttpClientService);

        // Act
        // Assert
        await Assert.ThrowsAsync<ExternalException>(async () =>
            await service.GetContentExcelVoidedTicket(1, 1, 1, "1"));
    }

    [Fact]
    public async Task GetVoidedTickets_ReturnExpectedResults()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeVoidBetAutomationInfo = new List<VoidBetAutomationInfo>
        {
            new() { VoidingHistoryId = 1, SiteDbGroupId = 1 }
        };
        var ticketsInServer = new List<TicketEmailModel> { new() };
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 1, false).ReturnsForAnyArgs(ticketsInServer);
        var service = new SendLetstalkServiceStageImp(default, default, voidBetAutomationService, default);
        var voidedTickets = new List<TicketEmailModel> { new() };

        // Act
        var actual = await service.GetVoidedTickets(fakeVoidBetAutomationInfo);

        // Assert
        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(1, 1, false);
        AssertExtension.Equal(voidedTickets, actual);
    }

    [Fact]
    public async Task GetVoidDataXml_ReturnExpectedResults()
    {
        // Arrange
        InitFakeServiceTests();
        repository.FetchAsync<VoidDataXmlModel>(default, default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        // Act
        var actual = await service.GetVoidDataXml(new List<int>());

        // Assert
        await repository.Received(1).FetchAsync<VoidDataXmlModel>(Arg.Any<GetVoidingHistoryCriteria>());
        Assert.Equal(new List<VoidDataXmlModel>(), actual);
    }

    [Fact]
    public void WriteLogLetstalk_LoggingServiceHasCalled()
    {
        // Arrange
        InitFakeServiceTests();
        var service = new SendLetstalkServiceStageImp(default, loggingService, default, default);

        // Act
        _ = service.WriteLogLetstalk(new List<Result> { new() }, new GroupVoidBetAutomationInfo(), new Stopwatch());

        // Assert
        loggingService.Received(1).InfoAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateLetstalk_RepositoryHasCalled()
    {
        // Arrange
        InitFakeServiceTests();
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        // Act
        await service.UpdateLetstalk(1, "", 1);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateLetstalkCommand>());
    }

    [Fact]
    public async Task InsertLetstalk_RepositoryHasCalled()
    {
        // Arrange
        InitFakeServiceTests();
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        // Act
        await service.InsertLetstalk(1, "", 1);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<InsertLetstalkCommand>());
    }
}