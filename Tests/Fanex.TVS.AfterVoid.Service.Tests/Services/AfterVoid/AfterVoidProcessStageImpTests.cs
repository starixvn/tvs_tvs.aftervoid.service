using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class AfterVoidProcessStageImpTests
{
    private readonly AfterVoidEmailService afterVoidEmailService;
    private readonly CashOutHttpClientService cashOutHttpClientService;
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendLetstalkServiceStage sendLetstalkServiceStage;
    private readonly SendMessageService sendMessageService;
    private readonly AfterVoidProcessStageImp service;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public AfterVoidProcessStageImpTests()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        sendMessageService = Substitute.For<SendMessageService>();
        afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
        cashOutHttpClientService = Substitute.For<CashOutHttpClientService>();
        service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService,
            afterVoidEmailService, loggingService,
            cashOutHttpClientService, sendLetstalkServiceStage);
    }

    [Fact]
    public async Task CreateMessageIdForAutoVoid_ThrowException()
    {
        // Arrange
        const string broadcastSessionId = "ADC-12AXX";
        var automationInfo = new VoidBetAutomationInfo { MessageId = 1 };
        sendMessageService.GetMessage(automationInfo.MessageId.Value).ReturnsForAnyArgs(new Message());
        voidBetAutomationService
            .UpdateMessageIdByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, 0)
            .Throws(new Exception());

        // Act
        var actual = await service.CreateMessageIdForAutoVoid(automationInfo, null, broadcastSessionId,
            new List<VoidDataXmlModel>(), 1, 1);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(),
            Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await sendLetstalkServiceStage.Received(1).InsertLetstalk(automationInfo.RequestId,
            automationInfo.BroadcastSessionId, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        Assert.Null(actual);
    }

    [Fact]
    public async Task CreateMessageIdForAutoVoid_HasMatch_CalledExpected()
    {
        // Arrange
        const string broadcastSessionId = "ADC-12AXX";
        var automationInfo = new VoidBetAutomationInfo { MessageId = 0, RequestInfo = new RequestDetailViewModel{PlayerNamesSelected = null}};
        var message = new Message { MessageId = 1 };
        var matchInfo = new Match { LeagueGroupId = 1, MatchId = 1 };
        sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, new List<VoidDataXmlModel>(), 1, 1, null)
            .ReturnsForAnyArgs(message);
        sendMessageService.InsertMessage(message, 1, 1, 1)
            .ReturnsForAnyArgs(1);

        // Act
        var actual = await service.CreateMessageIdForAutoVoid(automationInfo, matchInfo, broadcastSessionId,
            new List<VoidDataXmlModel>(), 1, 1);

        // Assert
        await sendMessageService.Received(1)
            .InitializeMessageContentForAutoVoid(matchInfo, Arg.Any<List<VoidDataXmlModel>>(), 1, 1, null);
        await sendMessageService.Received(1).InsertMessage(message, 1, 1, 1);
        await voidBetAutomationService.Received(1).UpdateMessageIdByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, message.MessageId);
        AssertExtension.Equal(message, actual);
    }

    [Fact]
    public async Task CreateMessageIdForAutoVoid_NoHasMatch_CalledExpected()
    {
        // Arrange
        const string broadcastSessionId = "ADC-12AXX";
        var automationInfo = new VoidBetAutomationInfo { MessageId = null, RequestInfo = new RequestDetailViewModel { PlayerNamesSelected = null } };
        var message = new Message { MessageId = 1 };
        sendMessageService.InitializeMessageContentForAutoVoid(null, new List<VoidDataXmlModel>(), 1, 1, null)
            .ReturnsForAnyArgs(message);
        sendMessageService.InsertMessage(message, 1, 1, 1)
            .ReturnsForAnyArgs(1);

        // Act
        var actual = await service.CreateMessageIdForAutoVoid(automationInfo, null, broadcastSessionId,
            new List<VoidDataXmlModel>(), 1, 1);

        // Assert
        await sendMessageService.Received(1)
            .InitializeMessageContentForAutoVoid(null, Arg.Any<List<VoidDataXmlModel>>(), 1, 1, null);
        await sendMessageService.Received(1).InsertMessage(message, 0, null, 1);
        await voidBetAutomationService.Received(1).UpdateMessageIdByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, message.MessageId);
        AssertExtension.Equal(message, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForAutoVoid_HasMatchInfo_ReturnNewMessage(int matchId)
    {
        // Arrange
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        sendMessageService.InitializeMessageContentForAutoVoid(default, default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);

        // Act
        var actual =
            await service.CreateMessageIdForAutoVoid(fakeAutomationInfo, new Match(), default, default, default,
                default);

        // Assert
        await sendMessageService.Received(1)
            .InsertMessage(Arg.Any<Message>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForAutoVoid_HasNotMatchInfo_ReturnNewMessage(int matchId)
    {
        // Arrange
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        sendMessageService.InitializeMessageContentForAutoVoid(default, default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);

        // Act
        var actual =
            await service.CreateMessageIdForAutoVoid(fakeAutomationInfo, null, default, default, default, default);

        // Assert
        await sendMessageService.Received(1)
            .InsertMessage(Arg.Any<Message>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Fact]
    public async Task CreateMessageIdForManualVoid_ReturnOldMessage()
    {
        // Arrange
        const int fakeMessageId = 113;
        var fakeAutomationInfo =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        sendMessageService.GetMessage(default).ReturnsForAnyArgs(fakeMessage);

        // Act
        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        // Assert
        await sendMessageService.Received(1).GetMessage(Arg.Is<int>(x => x == fakeMessageId));
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForManualVoid_ReturnNewMessage(int matchId)
    {
        // Arrange
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        sendMessageService.InitializeMessageContentForManualVoid(default).ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);

        // Act
        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Fact]
    public async Task CreateMessageIdForManualVoid_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();

        // Act
        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.Null(actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ReturnOldReason()
    {
        // Arrange
        const int fakeReasonId = 221;
        var fakeAutomationInfo =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        sendMessageService.GetVoidedReason(default).ReturnsForAnyArgs(fakeVoidedReason);

        // Act
        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        // Assert
        await sendMessageService.Received(1).GetVoidedReason(Arg.Is<int>(x => x == fakeReasonId));
        Assert.StrictEqual(fakeVoidedReason, actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ReturnNewReason()
    {
        // Arrange
        const int fakeReasonId = 221;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
            { EventByMatch = new RequestEventModel { SubIssueTypeId = 11111 } };
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        sendMessageService.InitializeVoidReason(default, default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        sendMessageService.InsertReason(default, default).ReturnsForAnyArgs(fakeReasonId);

        // Act
        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.StrictEqual(fakeVoidedReason, actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();

        // Act
        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(
            Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.Null(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ShouldReturnOldStatus()
    {
        var fakeAutomationInfo =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = true).Build();

        // Act
        var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ShouldReturnNewStatus()
    {
        // Arrange
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
        var fakeVoidData = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = false).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.IsAutoVoid = true;

        // Act
        var actual = await service.SendBatchMessage(fakeAutomationInfo, fakeEmails, 0, fakeVoidData);

        // Assert
        await sendMessageService.Received(1)
            .SendMessage(Arg.Is<IEnumerable<TicketEmailModel>>(x => x.Equals(fakeEmails)), Arg.Any<int>(),
                Arg.Any<int>(), Arg.Any<int>());
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        fakeAutomationInfo.IsAutoVoid = false;

        // Act
        var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(
            Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ReturnOldStatus()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentMessageToAdminSite = true)
            .Build();

        // Act
        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ReturnNewStatus()
    {
        // Arrange
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.IsSentMessageToAdminSite = false).Build();

        // Act
        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, fakeMessage);

        // Assert
        await sendMessageService.Received(1).InsertMessageIntoAdminSite(Arg.Is<Message>(x => x.Equals(fakeMessage)));
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        sendMessageService.InsertMessageIntoAdminSite(Arg.Any<Message>()).Throws(new Exception());

        // Act
        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(
            Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ReturnOldStatus()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.IsInsertedVoidedTransaction = true).Build();

        // Act
        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ReturnNewStatus()
    {
        // Arrange
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.IsInsertedVoidedTransaction = false).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 11111 },
            Request = new RequestModel { IssueTypeId = 11111 }
        };

        // Act
        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, fakeEmails, 0, 0);

        // Assert
        await sendMessageService.Received(1).InsertVoidedTransaction(Arg.Any<IEnumerable<TicketEmailModel>>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();

        // Act
        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(
            Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_ReturnOldStatus()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = true)
            .Build();

        // Act
        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, default);

        // Assert
        Assert.True(actual);
    }

    [Theory]
    [InlineData(IssueTypeConstant.DangerAccepted)]
    [InlineData(IssueTypeConstant.AbnormalBet)]
    [InlineData(IssueTypeConstant.AbnormalAccount)]
    public async Task UpdateTicketVoidingHistory_ReturnNewStatus(int issueTypeId)
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x =>
        {
            x.IsUpdatedVoidingHistory = false;
            x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel
                {
                    IssueTypeId = issueTypeId
                }
            };

            return 0;
        }).Build();
        var emailTickets = new List<TicketEmailModel>
        {
            new()
        };

        // Act
        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, emailTickets);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false)
            .Build();
        voidBetAutomationService.When(x => x.UpdateTicketVoidingHistory(Arg.Any<int>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>()))
            .Do(x => throw new Exception());

        // Act
        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, default);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(
            Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1)
            .UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task SendEmail_ReturnOldStatus()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentEmail = true).Build();

        // Act
        var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, null, null, null, null);

        // Assert
        Assert.True(actualStatus);
        Assert.Empty(actualResults);
    }

    [Fact]
    public async Task SendEmail_ReturnNewStatus()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false)
            .Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        afterVoidEmailService.Execute(Arg.Is<VoidBetAutomationInfo>(x => x.Equals(fakeAutomationInfo)),
            Arg.Any<IEnumerable<TicketEmailModel>>(), Arg.Any<Match>(), Arg.Any<VoidedReason>(),
            Arg.Any<Message>()).ReturnsForAnyArgs(fakeResults);

        // Act
        var (actualStatus, actualResults) =
            await service.SendEmail(fakeAutomationInfo, default, default, default, default);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actualStatus);
        Assert.StrictEqual(fakeResults, actualResults);
    }

    [Fact]
    public async Task SendEmail_ThrowException()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        voidBetAutomationService.UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>())
            .Throws(new Exception());

        // Act
        var (actualStatus, actualResults) =
            await service.SendEmail(fakeAutomationInfo, default, default, default, default);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Any<int>(),
            Arg.Any<int>(), ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Any<int>());
        Assert.False(actualStatus);
        Assert.Empty(actualResults);
    }

    [Fact]
    public async Task UpdateEmailResult_ShouldNotCallUpdateService()
    {
        // Arrange
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();

        // Act
        await service.UpdateEmailResult(default, fakeResults);

        // Assert
        await voidBetAutomationService.Received(0)
            .UpdateEmailResultByVoidingHistory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateEmailResult_ShouldCallUpdateService()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Random(1)
            .With(x => x.ErrorCode = ErrorCode.SendEmailToLicenseeSuccess).Build();

        // Act
        await service.UpdateEmailResult(fakeAutomationInfo, fakeResults);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateEmailResultByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId), Arg.Any<string>());
    }

    [Fact]
    public async Task WriteLogAfterVoid_ShouldCallLoggingService()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();

        // Act
        await service.WriteLogAfterVoid(fakeResults, fakeAutomationInfo);

        // Assert
        await loggingService.Received(1)
            .InfoAsync(Arg.Any<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task NotifyVoidResultsSignalR_ShouldCallNotificationSignalRService(int matchId)
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            MatchId = matchId
        };
        var currentEmailResults = new List<Result>
        {
            new() { ErrorCode = ErrorCode.LicenseeSuccess, ErrorMsg = "", Data = "" },
            new() { ErrorCode = ErrorCode.LicenseeFail, ErrorMsg = "", Data = "" }
        };
        var emailResultsHistory = new List<EmailResultModel>
        {
            new() { Type = ErrorCode.LicenseeSuccess, Message = "" },
            new() { Type = ErrorCode.LicenseeFail, Message = "" }
        };

        // Act
        await service.NotifyVoidResultsSignalR(automationInfo, currentEmailResults, "", emailResultsHistory);

        // Assert
        await notificationSignalRService.Received(1).NotifyVoidResultToUser(Arg.Any<string>(), Arg.Any<List<Result>>());
    }

    [Fact]
    public async Task CallCashOutApi_ReturnWhenIsRunExternalServiceTrue()
    {
        // Arrange
        var automationInfo =
            Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = true).Build();

        // Act
        await service.CallCashOutApi(automationInfo, null);

        // Assert
        await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateAfterVoidStep(1, 1, null);
    }

    [Fact]
    public async Task CallCashOutApi_ReturnWhenHaveNotBasketballCashOutTickets()
    {
        // Arrange
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = false).With(x => x.SportType = SportTypeConstant.Basketball).Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false)
            .Build();

        // Act
        await service.CallCashOutApi(fakeAutomationInfo, fakeEmails);

        // Assert
        await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateAfterVoidStep(1, 1, null);
    }

    [Fact]
    public async Task CallCashOutApi_CalledUpdateAfterVoidStep()
    {
        // Arrange
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = true).With(x => x.SportType = SportTypeConstant.Basketball).Build();

        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false)
            .Build();

        // Act
        await service.CallCashOutApi(fakeAutomationInfo, fakeEmails);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task CallCashOutApi_ThrowException()
    {
        // Arrange
        var emails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = true).With(x => x.SportType = SportTypeConstant.Basketball).Build();

        var automationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false)
            .Build();
        cashOutHttpClientService.VoidBet(Arg.Any<CashOutApiModel>()).Throws(new Exception());

        // Act
        await service.CallCashOutApi(automationInfo, emails);

        // Assert
        await loggingService.Received(1).ErrorAsync(Arg.Any<string>(), Arg.Any<Exception>());
    }
}