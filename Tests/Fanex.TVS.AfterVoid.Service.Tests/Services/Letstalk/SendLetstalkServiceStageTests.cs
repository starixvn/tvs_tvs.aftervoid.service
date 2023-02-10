using System.Collections.Generic;
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
    [Fact]
    public async Task GetVoidBetAutomationRunLetstalk_ReturnExpectedResults()
    {
        var voidBetAutomationInfoList = new List<VoidBetAutomationInfo>();
        var repository = Substitute.For<IDynamicRepository>();
        repository.FetchAsync<VoidBetAutomationInfo>(default).ReturnsForAnyArgs(voidBetAutomationInfoList);
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        var actual = await service.GetVoidBetAutomationRunLetstalk();

        await repository.Received(1).FetchAsync<VoidBetAutomationInfo>(Arg.Any<GetAllVoidBetAutomationInfoCriteria>());
        Assert.Equal(voidBetAutomationInfoList, actual);
    }

    [Fact]
    public async Task GetContentExcelVoidedTicket_ReturnExpectedResults()
    {
        var fakeResult = new byte[] { 1, 2 };
        var tvsClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        tvsClientService.GetContentExcelVoidedTicket(default, default, default, default).ReturnsForAnyArgs((true, fakeResult));
        var service = new SendLetstalkServiceStageImp(default, default, default, tvsClientService);

        var actual = await service.GetContentExcelVoidedTicket(default, default, default, default);

        await tvsClientService.Received(1).GetContentExcelVoidedTicket(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        Assert.Equal(fakeResult, actual);
    }

    [Fact]
    public async Task GetContentExcelVoidedTicket_ThrowException()
    {
        var tvsClientService = Substitute.For<TicketVoidingSystemHttpClientService>();
        tvsClientService.GetContentExcelVoidedTicket(default, default, default, default).ReturnsForAnyArgs((false, null));
        var service = new SendLetstalkServiceStageImp(default, default, default, tvsClientService);

        await Assert.ThrowsAsync<ExternalException>(async () =>
            await service.GetContentExcelVoidedTicket(default, default, default, default));
    }

    [Fact]
    public async Task GetVoidedTickets_ReturnExpectedResults()
    {
        var fakeVoidBetAutomationInfo = new List<VoidBetAutomationInfo>
        {
            new()
        };
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(default, default).ReturnsForAnyArgs(new List<TicketEmailModel>());
        var service = new SendLetstalkServiceStageImp(default, default, voidBetAutomationService, default);

        var actual = await service.GetVoidedTickets(fakeVoidBetAutomationInfo);

        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(Arg.Any<int>(), Arg.Any<int>());
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task GetVoidDataXml_ReturnExpectedResults()
    {
        var fakeVoidDataXml = new List<VoidDataXmlModel>();
        var repository = Substitute.For<IDynamicRepository>();
        repository.FetchAsync<VoidDataXmlModel>(default, default).ReturnsForAnyArgs(fakeVoidDataXml);
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        var actual = await service.GetVoidDataXml(new List<int>());

        await repository.Received(1).FetchAsync<VoidDataXmlModel>(Arg.Any<GetVoidingHistoryCriteria>());
        Assert.Equal(fakeVoidDataXml, actual);
    }

    [Fact]
    public void WriteLogLetstalk_ReturnExpectedResults()
    {
        var loggingService = Substitute.For<LoggingService>();
        var service = new SendLetstalkServiceStageImp(default, loggingService, default, default);

        service.WriteLogLetstalk(new List<Result> { new() }, new GroupVoidBetAutomationInfo());

        loggingService.Received(1).Info(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateLetstalk_ReturnExpectedResults()
    {
        var repository = Substitute.For<IDynamicRepository>();
        var service = new SendLetstalkServiceStageImp(repository, default, default, default);

        await service.UpdateLetstalk(new GroupVoidBetAutomationInfo(), 1, 1);

        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateLetstalkCommand>());
    }
}