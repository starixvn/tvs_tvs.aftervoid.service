using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class AfterVoidEmailServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly AfterVoidEmailServiceImp service;
    private readonly TVSWebSiteHttpClientService tvsNewSiteHttpClientService;

    public AfterVoidEmailServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        tvsNewSiteHttpClientService = Substitute.For<TVSWebSiteHttpClientService>();
        service = new AfterVoidEmailServiceImp(tvsNewSiteHttpClientService, repository);
    }

    [Fact]
    public async Task Execute_ReturnExpectedResults()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = 11 },
            EventByMatch = new RequestEventModel { SubIssueTypeId = 20 }
        };
        var fakeTicketEmails = Builder<TicketEmailModel>.CreateListOfSize(3).Build();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var fakeSubIssueTypes = Builder<SubIssueType>.CreateListOfSize(2).Random(1).With(x => x.SubIssueTypeId = 20)
            .Build();
        var parameter = new SendEmailStageParameter
        {
            AutomationInfo = fakeAutomationInfo,
            VoidReason = fakeReason,
            MatchInfo = fakeMatch,
            NormalMessage = fakeMessage,
            EmailTickets = fakeTicketEmails
        };
        repository.FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>()).ReturnsForAnyArgs(fakeSubIssueTypes);
        tvsNewSiteHttpClientService.SendEmail(Arg.Any<SendEmailApiModel>()).ReturnsForAnyArgs(
            (true, fakeResults));

        // Act
        var actual = await service.Execute(parameter);

        // Assert
        await repository.Received(1).FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>());
        Assert.Equal(fakeResults, actual);
    }

    [Fact]
    public async Task Execute_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel { IssueTypeId = 11 },
            EventByMatch = new RequestEventModel { SubIssueTypeId = 20 }
        };
        var fakeTicketEmails = Builder<TicketEmailModel>.CreateListOfSize(3).Build();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var fakeSubIssueTypes = Builder<SubIssueType>.CreateListOfSize(2).Random(1).With(x => x.SubIssueTypeId = 20)
            .Build();
        var parameter = new SendEmailStageParameter
        {
            AutomationInfo = fakeAutomationInfo,
            VoidReason = fakeReason,
            MatchInfo = fakeMatch,
            NormalMessage = fakeMessage,
            EmailTickets = fakeTicketEmails
        };
        repository.FetchAsync<SubIssueType>(Arg.Any<GetSubIssueTypeCriteria>()).ReturnsForAnyArgs(fakeSubIssueTypes);
        tvsNewSiteHttpClientService.SendEmail(Arg.Any<SendEmailApiModel>()).ReturnsForAnyArgs(
            (false, fakeResults));

        // Act
        // Assert
        await Assert.ThrowsAsync<ExternalException>(async () =>
            await service.Execute(parameter));
    }
}