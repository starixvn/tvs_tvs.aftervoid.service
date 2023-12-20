//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Fanex.Data.Repository;
//using Fanex.TVS.AfterVoid.Service.Constants;
//using Fanex.TVS.AfterVoid.Service.Criteria;
//using Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb;
//using Fanex.TVS.AfterVoid.Service.Models;
//using Fanex.TVS.AfterVoid.Service.Services;
//using NSubstitute;
//using TicketVoidingSystem.Constants;
//using TicketVoidingSystem.Models;
//using Xunit;

//namespace Fanex.TVS.AfterVoid.Service.Tests.Services;

//public class VoidBetAutomationServiceTests
//{
//    [Fact]
//    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusFailed()
//    {
//        // Arrange
//        var voidingHistoryModels = new List<VoidingHistoryModel>
//        {
//            new() { VoidingHistoryStatusId = ExecuteStatus.Failed },
//            new() { AfterVoidStatusId = ExecuteStatus.Failed }
//        };
//        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
//                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
//            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidBetAutomationRunAfterVoid();

//        // Assert
//        Assert.StrictEqual(automationInfo.First(), actual.Item1);
//        Assert.Equal(RequestStatusConstant.Failed, actual.Item3);
//    }

//    [Fact]
//    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusRunning()
//    {
//        // Arrange
//        var voidingHistoryModels = new List<VoidingHistoryModel>
//        {
//            new() { VoidingHistoryStatusId = ExecuteStatus.Running },
//            new() { VoidingHistoryStatusId = ExecuteStatus.Running }
//        };
//        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
//                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
//            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidBetAutomationRunAfterVoid();

//        // Assert
//        Assert.StrictEqual(automationInfo.First(), actual.Item1);
//        Assert.Equal(RequestStatusConstant.Running, actual.Item3);
//    }

//    [Fact]
//    public async Task GetVoidBetAutomationRunAfterVoid_ReturnModelWithCurrentRequestStatusCompleted()
//    {
//        // Arrange
//        var voidingHistoryModels = new List<VoidingHistoryModel>
//        {
//            new() { VoidingHistoryStatusId = ExecuteStatus.Done },
//            new() { AfterVoidStatusId = ExecuteStatus.Done }
//        };
//        var automationInfo = new List<VoidBetAutomationInfo> { new() { RequestId = 2 } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchMultipleAsync<VoidBetAutomationInfo, VoidingHistoryModel, EmailResultModel>(
//                Arg.Any<GetAutomationInfoAfterVoidCriteria>())
//            .ReturnsForAnyArgs((automationInfo, voidingHistoryModels, new List<EmailResultModel>()));
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidBetAutomationRunAfterVoid();

//        // Assert
//        Assert.StrictEqual(automationInfo.First(), actual.Item1);
//        Assert.Equal(RequestStatusConstant.Completed, actual.Item3);
//    }

//    [Theory]
//    [InlineData(IssueTypeConstant.WrongHDP)]
//    [InlineData(IssueTypeConstant.Wrong1stHDP)]
//    [InlineData(IssueTypeConstant.WrongOU)]
//    [InlineData(IssueTypeConstant.Wrong1stOU)]
//    [InlineData(IssueTypeConstant.DangerAccepted)]
//    [InlineData(IssueTypeConstant.RedCard)]
//    [InlineData(IssueTypeConstant.NotParticipate)]
//    public async Task GetRequestById_ReturnDataEventWithExpectedMatch(int issueTypeId)
//    {
//        // Arrange
//        var fakeRequests = new List<RequestModel>
//        {
//            new() { IssueTypeId = issueTypeId },
//            new() { IssueTypeId = issueTypeId }
//        };
//        var fakeRequestEvents = new List<RequestEventModel>
//        {
//            new() { MatchId = 100 },
//            new() { MatchId = 113 }
//        };
//        var message = new List<Message>
//        {
//            new() { PlayerNamesEnglish = "a & b", PlayerNamesChineseTraditional = "a & b", PlayerNamesChineseSimplified = "a & b" }
//        };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(Arg.Any<SelectRequestCriteria>())
//            .ReturnsForAnyArgs((fakeRequests, fakeRequestEvents, message));
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetRequestById(0, 100);

//        // Assert
//        AssertExtension.Equal(fakeRequests[0], actual.Request);
//        AssertExtension.Equal(fakeRequestEvents[0], actual.EventByMatch);
//    }

//    [Fact]
//    public async Task GetRequestById_ReturnDataWithFirstEvent()
//    {
//        // Arrange
//        var fakeRequests = new List<RequestModel>
//        {
//            new() { IssueTypeId = IssueTypeConstant.AbnormalBet },
//            new() { IssueTypeId = IssueTypeConstant.DangerAccepted }
//        };
//        var fakeRequestEvents = new List<RequestEventModel>
//        {
//            new() { MatchId = 100 },
//            new() { MatchId = 113 }
//        };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchMultipleAsync<RequestModel, RequestEventModel, Message>(Arg.Any<SelectRequestCriteria>())
//            .ReturnsForAnyArgs((fakeRequests, fakeRequestEvents, null));
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetRequestById(0, 113);

//        // Assert
//        Assert.StrictEqual(fakeRequests[0], actual.Request);
//        Assert.StrictEqual(fakeRequestEvents[0], actual.EventByMatch);
//    }

//    [Fact]
//    public async Task GetRequestSimpleInfo_ReturnExpectedData()
//    {
//        // Arrange
//        const int fakeRequestId = 113;
//        const int fakeVoidingHistoryId = 211;
//        var expectedRequestInfo = new RequestSimpleInfo { MatchId = 111 };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.GetAsync<RequestSimpleInfo>(
//                Arg.Is<GetRequestSimpleInfoCriteria>(x => x.RequestId == fakeRequestId))
//            .ReturnsForAnyArgs(expectedRequestInfo);
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetRequestSimpleInfo(fakeRequestId, fakeVoidingHistoryId);

//        // Assert
//        Assert.StrictEqual(expectedRequestInfo, actual);
//    }

//    [Fact]
//    public async Task UpdateServiceStatusByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
//    {
//        // Arrange
//        const int fakeRequestId = 113;
//        const int fakeVoidingHistoryId = 211;
//        const int fakeStatusId = 311;
//        var repository = Substitute.For<IDynamicRepository>();
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(fakeRequestId, fakeVoidingHistoryId,
//            fakeStatusId);

//        // Assert
//        await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
//            x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateStatusByVoidingHistory &&
//            x.RequestId == fakeRequestId
//            && x.VoidingHistoryId == fakeVoidingHistoryId && x.StatusId == fakeStatusId));
//    }

//    [Fact]
//    public async Task UpdateMessageIdByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
//    {
//        // Arrange
//        const int fakeRequestId = 113;
//        const int fakeVoidingHistoryId = 211;
//        const int fakeMessageId = 331;
//        var repository = Substitute.For<IDynamicRepository>();
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        await voidBetAutomationService.UpdateMessageIdByVoidingHistory(fakeRequestId, fakeVoidingHistoryId,
//            fakeMessageId);

//        // Assert
//        await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
//            x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateMessageIdByVoidingHistory &&
//            x.RequestId == fakeRequestId
//            && x.VoidingHistoryId == fakeVoidingHistoryId && x.MessageId == fakeMessageId));
//    }

//    [Fact]
//    public async Task UpdateAfterVoidStep_ShouldCallUpdateVoidBetAutomationServiceInfo()
//    {
//        // Arrange
//        const int fakeRequestId = 211;
//        const int fakeVoidingHistoryId = 331;
//        var fakeAfterVoidHistory = new VoidBetAutomationInfo { ReasonId = 444 };
//        var repository = Substitute.For<IDynamicRepository>();
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        await voidBetAutomationService.UpdateAfterVoidStep(fakeRequestId, fakeVoidingHistoryId, fakeAfterVoidHistory);

//        // Assert
//        await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
//            x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateAfterVoidStep && x.RequestId == fakeRequestId
//            && x.VoidingHistoryId == fakeVoidingHistoryId && x.ReasonId == fakeAfterVoidHistory.ReasonId));
//    }

//    [Fact]
//    public async Task UpdateEmailResultByVoidingHistory_ShouldCallUpdateVoidBetAutomationServiceInfo()
//    {
//        // Arrange
//        const int fakeRequestId = 211;
//        const int fakeVoidingHistoryId = 331;
//        const string fakeVoidResultContent = "abc";
//        var repository = Substitute.For<IDynamicRepository>();
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        await voidBetAutomationService.UpdateEmailResultByVoidingHistory(fakeRequestId, fakeVoidingHistoryId,
//            fakeVoidResultContent);

//        // Assert
//        await repository.Received(1).ExecuteAsync(Arg.Is<UpdateVoidBetAutomationInfoCriteria>(x =>
//            x.UpdateType == VoidBetAutomationServiceUpdateType.UpdateEmailsResultByVoidingHistory &&
//            x.RequestId == fakeRequestId
//            && x.VoidingHistoryId == fakeVoidingHistoryId && x.VoidResults == fakeVoidResultContent));
//    }

//    [Fact]
//    public async Task GetVoidedTicketsByVoidingHistory_ReturnExpectedModel()
//    {
//        // Arrange
//        var fakeTicketEmail = new List<TicketEmailModel> { new() { BetId = 113 } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchAsync<TicketEmailModel>(Arg.Any<GetTicketsEmailCriteria>())
//            .ReturnsForAnyArgs(fakeTicketEmail);
//        repository.GetAsync<CheckRequestOfITUserModel>(Arg.Any<GetTicketsEmailCriteria>())
//            .ReturnsForAnyArgs(new CheckRequestOfITUserModel());
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 1, true);

//        // Assert
//        AssertExtension.Equal(fakeTicketEmail, actual);
//    }

//    [Fact]
//    public async Task GetVoidedTicketsByVoidingHistory_NoHasCheckITUser_ReturnExpectedModel()
//    {
//        // Arrange
//        var fakeTicketEmail = new List<TicketEmailModel> { new() { BetId = 113 } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchAsync<TicketEmailModel>(Arg.Any<GetTicketsEmailCriteria>())
//            .ReturnsForAnyArgs(fakeTicketEmail);
//        repository.GetAsync<CheckRequestOfITUserModel>(Arg.Any<GetTicketsEmailCriteria>())
//            .ReturnsForAnyArgs((CheckRequestOfITUserModel)null);
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 1, true);

//        // Assert
//        AssertExtension.Equal(fakeTicketEmail, actual);
//    }
//    [Fact]
//    public async Task GetVoidDataFromVoidingHistory_ReturnExpectedModel()
//    {
//        // Arrange
//        const int fakeVoidingHistoryId = 331;
//        var fakeVoidDataXml = new List<VoidDataXmlModel> { new() { ContentEnglish = "zbc" } };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.FetchAsync<VoidDataXmlModel>(Arg.Any<GetVoidingHistoryCriteria>())
//            .ReturnsForAnyArgs(fakeVoidDataXml);
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetVoidDataFromVoidingHistory(fakeVoidingHistoryId);

//        // Assert
//        AssertExtension.Equal(fakeVoidDataXml, actual);
//    }

//    [Fact]
//    public async Task GetMatchInfo_ReturnExpectedModel()
//    {
//        // Arrange
//        const int fakeMatchId = 331;
//        var fakeMatch = new Match { MatchCode = "ppp00" };
//        var repository = Substitute.For<IDynamicRepository>();
//        repository.GetAsync<Match>(
//            Arg.Is<GetMatchInfoCriteria>(x => x.MatchId == fakeMatchId)).ReturnsForAnyArgs(fakeMatch);
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        var actual = await voidBetAutomationService.GetMatchInfo(fakeMatchId);

//        // Assert
//        Assert.StrictEqual(fakeMatch, actual);
//    }

//    [Fact]
//    public async Task UpdateTicketVoidingHistory_ShouldCallExecuteAsync()
//    {
//        // Arrange
//        const byte fakeStatusId = 1;
//        const int fakeVoidingHistoryId = 331;
//        const int fakeReasonId = 111;
//        const int fakeMessageId = 222;
//        var repository = Substitute.For<IDynamicRepository>();
//        var voidBetAutomationService = new VoidBetAutomationServiceImp(repository);

//        // Act
//        await voidBetAutomationService.UpdateTicketVoidingHistory(fakeVoidingHistoryId, fakeStatusId, fakeReasonId,
//            fakeMessageId, null, null, null);

//        // Assert
//        await repository.Received(1).ExecuteAsync(Arg.Is<UpdateHistoryCommand>(x =>
//            x.StatusId == fakeStatusId && x.VoidingHistoryId == fakeVoidingHistoryId
//                                       && x.ReasonId == fakeReasonId && x.MessageId == fakeMessageId));
//    }
//}