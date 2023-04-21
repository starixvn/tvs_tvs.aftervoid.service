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
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class AfterVoidProcessStageImpTests
{
    [Fact]
    public async Task CreateMessageIdForAutoVoid_ThrowException()
    {
        const string fakeSessionId = "ADC-12AXX";
        var fakeAutomation = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, sendLetstalkServiceStage);

        var actual = await service.CreateMessageIdForAutoVoid(fakeAutomation, null, fakeSessionId, false, 0, null);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await notificationSignalRService.Received().UpdateFailedStatusForRequest(Arg.Any<int>());
        Assert.Null(actual);
    }

    [Fact]
    public async Task CreateMessageIdForAutoVoid_ReturnOldMessage()
    {
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService.GetMessage(default).ReturnsForAnyArgs(fakeMessage);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, null, null, null, null);

        var actual = await service.CreateMessageIdForAutoVoid(fakeAutomationInfo, null, default, default, default, default);

        await sendMessageService.Received(1).GetMessage(Arg.Is<int>(x => x == fakeMessageId));
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForAutoVoid_HasMatchInfo_ReturnNewMessage(int matchId)
    {
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        sendMessageService.InitializeMessageContentForAutoVoid(default, default, default, default).ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.CreateMessageIdForAutoVoid(fakeAutomationInfo, new Match(), default, default, default, default);

        await sendMessageService.Received(1).InsertMessage(Arg.Any<Message>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForAutoVoid_HasNotMatchInfo_ReturnNewMessage(int matchId)
    {
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        sendMessageService.InitializeMessageContentForAutoVoid(default, default, default, default).ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.CreateMessageIdForAutoVoid(fakeAutomationInfo, null, default, default, default, default);

        await sendMessageService.Received(1).InsertMessage(Arg.Any<Message>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Fact]
    public async Task CreateMessageIdForManualVoid_ReturnOldMessage()
    {
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService.GetMessage(default).ReturnsForAnyArgs(fakeMessage);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, null, null, null, null);

        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        await sendMessageService.Received(1).GetMessage(Arg.Is<int>(x => x == fakeMessageId));
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CreateMessageIdForManualVoid_ReturnNewMessage(int matchId)
    {
        const int fakeMessageId = 113;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.MatchId = matchId;
        var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
        var fakeMatchInfo = Builder<Match>.CreateNew().Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        sendMessageService.InitializeMessageContentForManualVoid(default).ReturnsForAnyArgs(fakeMessage);
        sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.StrictEqual(fakeMessage, actual);
    }

    [Fact]
    public async Task CreateMessageIdForManualVoid_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.Null(actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ReturnOldReason()
    {
        const int fakeReasonId = 221;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        sendMessageService.GetVoidedReason(default).ReturnsForAnyArgs(fakeVoidedReason);
        var service = new AfterVoidProcessStageImp(null, sendMessageService, null, null, null, null, null);

        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        await sendMessageService.Received(1).GetVoidedReason(Arg.Is<int>(x => x == fakeReasonId));
        Assert.StrictEqual(fakeVoidedReason, actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ReturnNewReason()
    {
        const int fakeReasonId = 221;
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel { EventByMatch = new() { SubIssueTypeId = 11111 } };
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        sendMessageService.InitializeVoidReason(default, default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        sendMessageService.InsertReason(default, default).ReturnsForAnyArgs(fakeReasonId);
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null,
            null, null, null, null);

        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.StrictEqual(fakeVoidedReason, actual);
    }

    [Fact]
    public async Task CreateVoidedReason_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.Null(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ShouldReturnOldStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = true).Build();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

        Assert.True(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ShouldReturnNewStatus()
    {
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
        var fakeVoidData = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = false).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 11111 } };
        fakeAutomationInfo.IsAutoVoid = true;
        var sendMessageService = Substitute.For<SendMessageService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.SendBatchMessage(fakeAutomationInfo, fakeEmails, 0, fakeVoidData);

        await sendMessageService.Received(1).SendMessage(Arg.Is<IEnumerable<TicketEmailModel>>(x => x.Equals(fakeEmails)), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task SendBatchMessage_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        fakeAutomationInfo.IsAutoVoid = false;
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ReturnOldStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentMessageToAdminSite = true).Build();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

        Assert.True(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ReturnNewStatus()
    {
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentMessageToAdminSite = false).Build();
        var sendMessageService = Substitute.For<SendMessageService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, fakeMessage);

        await sendMessageService.Received(1).InsertMessageIntoAdminSite(Arg.Is<Message>(x => x.Equals(fakeMessage)));
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task SendMessageToAdminSite_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ReturnOldStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsInsertedVoidedTransaction = true).Build();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

        Assert.True(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ReturnNewStatus()
    {
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsInsertedVoidedTransaction = false).Build();
        fakeAutomationInfo.RequestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 11111 },
            Request = new RequestModel { IssueTypeId = 11111 }
        };
        var sendMessageService = Substitute.For<SendMessageService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, sendMessageService, null, null, null, null, null);

        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, fakeEmails, 0, 0);

        await sendMessageService.Received(1).InsertVoidedTransaction(Arg.Any<IEnumerable<TicketEmailModel>>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_ReturnOldStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = true).Build();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, default);

        Assert.True(actual);
    }

    [Theory]
    [InlineData(IssueTypeConstant.DangerAccepted)]
    [InlineData(IssueTypeConstant.AbnormalBet)]
    public async Task UpdateTicketVoidingHistory_ReturnNewStatus(int issueTypeId)
    {
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
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, null, null, null, null, null);

        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, emailTickets);

        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false).Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.When(x => x.UpdateTicketVoidingHistory(Arg.Any<int>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>()))
            .Do(x => throw new Exception());
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0, default);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actual);
    }

    [Fact]
    public async Task SendEmail_ReturnOldStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentEmail = true).Build();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, null, null, null, null);

        Assert.True(actualStatus);
        Assert.Empty(actualResults);
    }

    [Fact]
    public async Task SendEmail_ReturnNewStatus()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false).Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
        afterVoidEmailService.Execute(Arg.Is<VoidBetAutomationInfo>(x => x.Equals(fakeAutomationInfo)),
            Arg.Any<IEnumerable<TicketEmailModel>>(), Arg.Any<Match>(), Arg.Any<VoidedReason>(),
            Arg.Any<Message>()).ReturnsForAnyArgs(fakeResults);
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, null, afterVoidEmailService, null, null, null);

        var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, default, default, default, default);

        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actualStatus);
        Assert.StrictEqual(fakeResults, actualResults);
    }

    [Fact]
    public async Task SendEmail_ThrowException()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, notificationSignalRService, null, loggingService, null, null);

        var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, default, default, default, default);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
            Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId),
            Arg.Is<int>(x => x == ExecuteStatus.Failed));
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
        Assert.False(actualStatus);
        Assert.Empty(actualResults);
    }

    [Fact]
    public async Task UpdateEmailResult_ShouldNotCallUpdateService()
    {
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, null, null, null, null, null);

        await service.UpdateEmailResult(default, fakeResults);

        await voidBetAutomationService.Received(0)
            .UpdateEmailResultByVoidingHistory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateEmailResult_ShouldCallUpdateService()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Random(1).With(x => x.ErrorCode = ErrorCode.SendEmailToLicenseeSuccess).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, null, null, null, null, null);

        await service.UpdateEmailResult(fakeAutomationInfo, fakeResults);

        await voidBetAutomationService.Received(1)
            .UpdateEmailResultByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistoryId), Arg.Any<string>());
    }

    [Fact]
    public void WriteLogAfterVoid_ShouldCallLoggingService()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
        var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
        var loggingService = Substitute.For<LoggingService>();
        var service = new AfterVoidProcessStageImp(null, null, null, null, loggingService, null, null);

        service.WriteLogAfterVoid(fakeResults, fakeAutomationInfo);

        loggingService.Received(1)
            .Info(Arg.Any<string>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task NotifyVoidResultsSignalR_ShouldCallNotificationSignalRService(int matchId)
    {
        var automationInfo = new VoidBetAutomationInfo
        {
            MatchId = matchId
        };
        var currentEmailResults = new List<Result>
        {
            new() { ErrorCode = ErrorCode.LicenseeSuccess, ErrorMsg = "", Data = ""},
            new() { ErrorCode = ErrorCode.LicenseeFail, ErrorMsg = "", Data = "" },
        };
        var emailResultsHistory = new List<EmailResultModel>
        {
            new() { Type = ErrorCode.LicenseeSuccess, Message = "" },
            new() { Type = ErrorCode.LicenseeFail, Message = "" },
        };
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var service = new AfterVoidProcessStageImp(null, null, notificationSignalRService, null, null, null, null);

        await service.NotifyVoidResultsSignalR(automationInfo, currentEmailResults, "", emailResultsHistory);

        await notificationSignalRService.Received(1).NotifyVoidResultToUser(Arg.Any<string>(), Arg.Any<List<Result>>());
    }

    [Fact]
    public async Task CallCashOutApi_ReturnWhenIsRunExternalServiceTrue()
    {
        var automationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = true).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        await service.CallCashOutApi(automationInfo, null);

        await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateAfterVoidStep(1, 1, null);
    }

    [Fact]
    public async Task CallCashOutApi_ReturnWhenHaveNotBasketballCashOutTickets()
    {
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = false).With(x => x.SportType = SportTypeConstant.Basketball).Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new AfterVoidProcessStageImp(null, null, null, null, null, null, null);

        await service.CallCashOutApi(fakeAutomationInfo, fakeEmails);

        await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateAfterVoidStep(1, 1, null);
    }

    [Fact]
    public async Task CallCashOutApi_CalledUpdateAfterVoidStep()
    {
        var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = true).With(x => x.SportType = SportTypeConstant.Basketball).Build();

        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var cashOutApi = Substitute.For<CashOutHttpClientService>();
        var service = new AfterVoidProcessStageImp(voidBetAutomationService, null, null, null, null, cashOutApi, null);

        await service.CallCashOutApi(fakeAutomationInfo, fakeEmails);

        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task CallCashOutApi_ThrowException()
    {
        var emails = Builder<TicketEmailModel>.CreateListOfSize(2)
            .All().With(x => x.IsCashOut = true).With(x => x.SportType = SportTypeConstant.Basketball).Build();

        var automationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsRunExternalService = false).Build();
        var cashOutApi = Substitute.For<CashOutHttpClientService>();
        var loggingService = Substitute.For<LoggingService>();
        cashOutApi.When(x => x.VoidBet(Arg.Any<CashOutApiModel>()))
            .Do(x => throw new Exception());
        var service = new AfterVoidProcessStageImp(null, null, null, null, loggingService, cashOutApi, null);

        await service.CallCashOutApi(automationInfo, emails);

        loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
    }
}