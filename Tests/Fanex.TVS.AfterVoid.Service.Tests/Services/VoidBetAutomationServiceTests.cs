using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

public class VoidBetAutomationServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly MatchService matchService;
    private VoidBetAutomationService Service;

    public VoidBetAutomationServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        matchService = Substitute.For<MatchService>();
    }

    [Fact]
    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusFailed()
    {
        // Arrange
        var voidingHistoryModels = new List<VoidingHistoryModel>
        {
            new() { VoidingHistoryStatusId = ExecuteStatus.Failed },
            new() { AfterVoidStatusId = ExecuteStatus.Failed }
        };
        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidBetAutomationRunAfterVoid();

        // Assert
        Assert.StrictEqual(automationInfo.First(), actual.Item1);
        Assert.Equal(RequestStatusConstant.Failed, actual.Item3);
    }

    [Fact]
    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusRunning()
    {
        // Arrange
        var voidingHistoryModels = new List<VoidingHistoryModel>
        {
            new() { VoidingHistoryStatusId = ExecuteStatus.Running },
            new() { VoidingHistoryStatusId = ExecuteStatus.Running }
        };
        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidBetAutomationRunAfterVoid();

        // Assert
        Assert.StrictEqual(automationInfo.First(), actual.Item1);
        Assert.Equal(RequestStatusConstant.Running, actual.Item3);
    }

    [Fact]
    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusCompleted()
    {
        // Arrange
        var voidingHistoryModels = new List<VoidingHistoryModel>
        {
            new() { VoidingHistoryStatusId = ExecuteStatus.Done },
            new() { AfterVoidStatusId = ExecuteStatus.Done }
        };
        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidBetAutomationRunAfterVoid();

        // Assert
        Assert.StrictEqual(automationInfo.First(), actual.Item1);
        Assert.Equal(RequestStatusConstant.Completed, actual.Item3);
    }

    [Theory]
    [InlineData(IssueTypeConstant.WrongHDP)]
    [InlineData(IssueTypeConstant.Wrong1stHDP)]
    [InlineData(IssueTypeConstant.WrongOU)]
    [InlineData(IssueTypeConstant.Wrong1stOU)]
    [InlineData(IssueTypeConstant.DangerAccepted)]
    [InlineData(IssueTypeConstant.RedCard)]
    [InlineData(IssueTypeConstant.NotParticipate)]
    public async Task GetRequestById_ReturnDataEventWithExpectedMatch(int issueTypeId)
    {
        // Arrange
        var fakeRequests = new List<RequestModel>
        {
            new() { IssueTypeId = issueTypeId },
            new() { IssueTypeId = issueTypeId }
        };
        var fakeRequestEvents = new List<RequestEventModel>
        {
            new() { MatchId = 100 },
            new() { MatchId = 113 }
        };
        var playerNames = new List<PlayerName>
        {
            new() { PlayerNamesEnglish = "a & b", PlayerNamesChineseTraditional = "a & b", PlayerNamesChineseSimplified = "a & b" }
        };
        repository.FetchMultipleAsync<RequestModel, RequestEventModel, PlayerName>(Arg.Any<SelectRequestCriteria>())
            .ReturnsForAnyArgs((fakeRequests, fakeRequestEvents, playerNames));
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetRequestById(0, 100);

        // Assert
        AssertExtension.Equal(fakeRequests[0], actual.Request);
        AssertExtension.Equal(fakeRequestEvents[0], actual.EventByMatch);
    }

    [Fact]
    public async Task GetRequestById_ReturnDataWithFirstEvent()
    {
        // Arrange
        var fakeRequests = new List<RequestModel>
        {
            new() { IssueTypeId = IssueTypeConstant.AbnormalBet },
            new() { IssueTypeId = IssueTypeConstant.DangerAccepted }
        };
        var fakeRequestEvents = new List<RequestEventModel>
        {
            new() { MatchId = 100 },
            new() { MatchId = 113 }
        };
        var playerNames = new List<PlayerName> { new() };
        repository.FetchMultipleAsync<RequestModel, RequestEventModel, PlayerName>(Arg.Any<SelectRequestCriteria>())
            .ReturnsForAnyArgs((fakeRequests, fakeRequestEvents, playerNames));
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetRequestById(0, 113);

        // Assert
        Assert.StrictEqual(fakeRequests[0], actual.Request);
        Assert.StrictEqual(fakeRequestEvents[0], actual.EventByMatch);
    }

    [Fact]
    public async Task GetRequestSimpleInfo_ReturnExpectedData()
    {
        // Arrange
        const int fakeRequestId = 113;
        const int fakeVoidingHistoryId = 211;
        var expectedRequestInfo = new RequestSimpleInfo { MatchId = 111 };
        repository.GetAsync<RequestSimpleInfo>(
                Arg.Is<GetRequestSimpleInfoCriteria>(x => x.RequestId == fakeRequestId))
            .ReturnsForAnyArgs(expectedRequestInfo);
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetRequestSimpleInfo(fakeRequestId, fakeVoidingHistoryId);

        // Assert
        Assert.StrictEqual(expectedRequestInfo, actual);
    }

    [Fact]
    public async Task UpdateServiceStatusByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
    {
        // Arrange
        const int fakeRequestId = 113;
        const int fakeVoidingHistoryId = 211;
        const int fakeStatusId = 311;
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        await Service.UpdateServiceStatusByVoidingHistory(fakeRequestId, fakeVoidingHistoryId,
            fakeStatusId);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateVoidBetAutomationInfoCriteria>());
    }

    [Fact]
    public async Task UpdateAfterVoidStep_ShouldCallUpdateVoidBetAutomationServiceInfo()
    {
        // Arrange
        const int fakeRequestId = 211;
        const int fakeVoidingHistoryId = 331;
        var fakeAfterVoidHistory = new VoidBetAutomationInfo { ReasonId = 444 };
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        await Service.UpdateAfterVoidStep(fakeRequestId, fakeVoidingHistoryId, fakeAfterVoidHistory);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateVoidBetAutomationInfoCriteria>());
    }

    [Fact]
    public async Task UpdateEmailResultByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
    {
        // Arrange
        const int fakeRequestId = 211;
        const int fakeVoidingHistoryId = 331;
        const string fakeVoidResultContent = "abc";
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        await Service.UpdateEmailResultByVoidingHistory(fakeRequestId, fakeVoidingHistoryId,
            fakeVoidResultContent);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateVoidBetAutomationInfoCriteria>());
    }

    [Fact]
    public async Task GetVoidedTicketsByVoidingHistory_ReturnExpectedModel()
    {
        // Arrange
        var fakeTicketEmail = new List<TicketEmailModel> { new() { BetId = 113 } };
        repository.FetchAsync<TicketEmailModel>(Arg.Any<GetTicketsEmailCriteria>())
            .ReturnsForAnyArgs(fakeTicketEmail);
        repository.GetAsync<CheckRequestOfITUserModel>(Arg.Any<GetTicketsEmailCriteria>())
            .ReturnsForAnyArgs(new CheckRequestOfITUserModel());
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidedTicketsByVoidingHistory(1, 1, true);

        // Assert
        AssertExtension.Equal(fakeTicketEmail, actual);
    }

    [Fact]
    public async Task GetVoidedTicketsByVoidingHistory_NoHasCheckITUser_ReturnExpectedModel()
    {
        // Arrange
        var fakeTicketEmail = new List<TicketEmailModel> { new() { BetId = 113 } };
        repository.FetchAsync<TicketEmailModel>(Arg.Any<GetTicketsEmailCriteria>())
            .ReturnsForAnyArgs(fakeTicketEmail);
        repository.GetAsync<CheckRequestOfITUserModel>(Arg.Any<GetTicketsEmailCriteria>())
            .ReturnsForAnyArgs((CheckRequestOfITUserModel)null);
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidedTicketsByVoidingHistory(1, 1, true);

        // Assert
        AssertExtension.Equal(fakeTicketEmail, actual);
    }

    [Fact]
    public async Task GetVoidDataFromVoidingHistory_ReturnExpectedModel()
    {
        // Arrange
        const int fakeVoidingHistoryId = 331;
        var fakeVoidDataXml = new List<VoidDataXmlModel> { new() { ContentEnglish = "zbc" } };
        repository.FetchAsync<VoidDataXmlModel>(Arg.Any<GetVoidingHistoryCriteria>())
            .ReturnsForAnyArgs(fakeVoidDataXml);
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetVoidDataFromVoidingHistory(fakeVoidingHistoryId);

        // Assert
        AssertExtension.Equal(fakeVoidDataXml, actual);
    }

    [Fact]
    public async Task GetMatchInfo_ReturnExpectedModel()
    {
        // Arrange
        const int fakeMatchId = 331;
        var fakeMatch = new Match { MatchCode = "ppp00" };
        repository.GetAsync<Match>(
            Arg.Is<GetMatchInfoCriteria>(x => x.MatchId == fakeMatchId)).ReturnsForAnyArgs(fakeMatch);
        matchService.UpdateMatchInfoFromRemotingRef(Arg.Any<Match>()).ReturnsForAnyArgs(fakeMatch);
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        var actual = await Service.GetMatchInfo(fakeMatchId);

        // Assert
        Assert.StrictEqual(fakeMatch, actual);
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_ShouldCallExecuteAsync()
    {
        // Arrange
        const int fakeVoidingHistoryId = 331;
        const int fakeReasonId = 111;
        const int fakeMessageId = 222;
        Service = new VoidBetAutomationServiceImp(repository, matchService);

        // Act
        await Service.UpdateTicketVoidingHistory(fakeVoidingHistoryId, fakeReasonId, fakeMessageId, 1);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<UpdateHistoryCommand>());
    }
}