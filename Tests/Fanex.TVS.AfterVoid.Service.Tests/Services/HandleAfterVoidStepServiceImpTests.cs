using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using FizzWare.NBuilder;
using NSubstitute;
using Xunit;

namespace Fanex.TVS.AfterVoid.Core.Services.Tests
{
    public class HandleAfterVoidStepServiceImpTests
    {
        [Fact()]
        public async Task CreateMessageIdForAllRequests_ReturnOldMessage()
        {
            var fakeMessageId = 113;
            var fakeSessionId = 998;
            var fakeAutomationList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.MessageId = null)
                .Random(1).With(x => x.MessageId = fakeMessageId).Build();
            var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.GetMessage(Arg.Any<int>()).Returns(fakeMessage);
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.CreateMessageIdForAllRequests(fakeAutomationList, null, fakeSessionId, false, 0, null);

            await voidBetAutomationService.Received(1).UpdateMessageIdBySession(Arg.Is<int>(x => x == fakeSessionId),
                Arg.Is<int>(x => x == fakeMessageId));
            Assert.StrictEqual(fakeMessage, actual);
        }

        [Fact()]
        public async Task CreateMessageIdForAllRequests_ReturnNewMessage()
        {
            var fakeMessageId = 113;
            var fakeSessionId = 998;
            var fakeAutomationList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.MessageId = null).Build();
            var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.InitializeMessageContentForAutoVoid(default, default, default, default).ReturnsForAnyArgs(fakeMessage);
            sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.CreateMessageIdForAllRequests(fakeAutomationList, null, fakeSessionId, false, 0, null);

            await voidBetAutomationService.Received(1).UpdateMessageIdBySession(Arg.Is<int>(x => x == fakeSessionId),
                Arg.Is<int>(x => x == fakeMessageId));
            Assert.StrictEqual(fakeMessage, actual);
        }

        [Fact()]
        public async Task CreateMessageIdForAllRequests_ThrowException()
        {
            var fakeSessionId = 998;
            var fakeAutomationList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.MessageId = null).Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.CreateMessageIdForAllRequests(fakeAutomationList, null, fakeSessionId, false, 0, null);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusBySession(Arg.Is<int>(x => x == fakeSessionId),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(3).UpdateFailedStatusForRequest(Arg.Any<int>());
            Assert.Null(actual);
        }

        [Fact()]
        public async Task CreateMessageIdForManualVoid_ReturnOldMessage()
        {
            var fakeMessageId = 113;
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
            var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.GetMessage(Arg.Any<int>()).Returns(fakeMessage);
            var service = new HandleAfterVoidStepServiceImp(null, sendMessageService, null, null, null, null);

            var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

            await sendMessageService.Received(1).GetMessage(Arg.Is<int>(x => x == fakeMessageId));
            Assert.StrictEqual(fakeMessage, actual);
        }

        [Fact()]
        public async Task CreateMessageIdForManualVoid_ReturnNewMessage()
        {
            var fakeMessageId = 113;
            var fakeSessionId = 998;
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
            fakeAutomationInfo.RequestInfo = new RequestDetailViewModel()
            { Request = new RequestModel() { IssueTypeId = 11111 } };
            var fakeMessage = Builder<Message>.CreateNew().With(x => x.MessageId = fakeMessageId).Build();
            var fakeMatchInfo = Builder<Match>.CreateNew().Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.InitializeMessageContentForManualVoid(default).ReturnsForAnyArgs(fakeMessage);
            sendMessageService.InsertMessage(default, default, default, default).ReturnsForAnyArgs(fakeMessageId);
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatchInfo);
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.StrictEqual(fakeMessage, actual);
        }

        [Fact()]
        public async Task CreateMessageIdForManualVoid_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.MessageId = null).Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.CreateMessageIdForManualVoid(fakeAutomationInfo, null);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.Null(actual);
        }

        [Fact()]
        public async Task CreateVoidedReason_ReturnOldReason()
        {
            var fakeReasonId = 221;
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.GetVoidedReason(default).ReturnsForAnyArgs(fakeVoidedReason);
            var service = new HandleAfterVoidStepServiceImp(null, sendMessageService, null, null, null, null);

            var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

            await sendMessageService.Received(1).GetVoidedReason(Arg.Is<int>(x => x == fakeReasonId));
            Assert.StrictEqual(fakeVoidedReason, actual);
        }

        [Fact()]
        public async Task CreateVoidedReason_ReturnNewReason()
        {
            var fakeReasonId = 221;
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();
            fakeAutomationInfo.RequestInfo = new RequestDetailViewModel()
            { EventByMatch = new() { SubIssueTypeId = 11111 } };
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().With(x => x.ReasonId = fakeReasonId).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            sendMessageService.InitializeVoidReason(default, default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            sendMessageService.InsertReason(default, default).ReturnsForAnyArgs(fakeReasonId);
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.StrictEqual(fakeVoidedReason, actual);
        }

        [Fact()]
        public async Task CreateVoidedReason_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.ReasonId = null).Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.CreateVoidedReason(fakeAutomationInfo, 0, null);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.Null(actual);
        }

        [Fact()]
        public async Task SendBatchMessage_ShouldReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

            Assert.True(actual);
        }

        [Fact()]
        public async Task SendBatchMessage_ShouldReturnNewStatus()
        {
            var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
            var fakeVoidData = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentBatchMessage = false).Build();
            fakeAutomationInfo.RequestInfo = new RequestDetailViewModel()
            { Request = new RequestModel() { IssueTypeId = 11111 } };
            var sendMessageService = Substitute.For<SendMessageService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.SendBatchMessage(fakeAutomationInfo, fakeEmails, 0, fakeVoidData);

            await sendMessageService.Received(1).SendMessage(Arg.Is<IEnumerable<TicketEmailModel>>(x => x.Equals(fakeEmails)), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actual);
        }

        [Fact()]
        public async Task SendBatchMessage_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.SendBatchMessage(fakeAutomationInfo, null, 0, null);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actual);
        }

        [Fact()]
        public async Task SendMessageToAdminSite_ReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentMessageToAdminSite = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

            Assert.True(actual);
        }

        [Fact()]
        public async Task SendMessageToAdminSite_ReturnNewStatus()
        {
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentMessageToAdminSite = false).Build();
            var sendMessageService = Substitute.For<SendMessageService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, fakeMessage);

            await sendMessageService.Received(1).InsertMessageIntoAdminSite(Arg.Is<Message>(x => x.Equals(fakeMessage)));
            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actual);
        }

        [Fact()]
        public async Task SendMessageToAdminSite_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.SendMessageToAdminSite(fakeAutomationInfo, null);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actual);
        }

        [Fact()]
        public async Task InsertVoidedTransaction_ReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsInsertedVoidedTransaction = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

            Assert.True(actual);
        }

        [Fact()]
        public async Task InsertVoidedTransaction_ReturnNewStatus()
        {
            var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsInsertedVoidedTransaction = false).Build();
            fakeAutomationInfo.RequestInfo = new RequestDetailViewModel()
            {
                EventByMatch = new() { SubIssueTypeId = 11111 },
                Request = new RequestModel() { IssueTypeId = 11111 }
            };
            var sendMessageService = Substitute.For<SendMessageService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, sendMessageService, null, null, null, null);

            var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, fakeEmails, 0, 0);

            await sendMessageService.Received(1).InsertVoidedTransaction(Arg.Is<IEnumerable<TicketEmailModel>>(x => x.Equals(fakeEmails)),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actual);
        }

        [Fact()]
        public async Task InsertVoidedTransaction_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.InsertVoidedTransaction(fakeAutomationInfo, null, 0, 0);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actual);
        }

        [Fact()]
        public async Task UpdateTicketVoidingHistory_ReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0);

            Assert.True(actual);
        }

        [Fact()]
        public async Task UpdateTicketVoidingHistory_ReturnNewStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, null, null, null, null);

            var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0);

            await voidBetAutomationService.Received(1).UpdateTicketVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory), Arg.Is<byte>(x => x == 2), Arg.Any<int>(), Arg.Any<int>());
            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actual);
        }

        [Fact()]
        public async Task UpdateTicketVoidingHistory_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.When(x => x.UpdateTicketVoidingHistory(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<int>(), Arg.Any<int>()))
                .Do(x => throw new Exception());
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var actual = await service.UpdateTicketVoidingHistory(fakeAutomationInfo, 0, 0);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actual);
        }

        [Fact()]
        public async Task SendEmail_ReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentEmail = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, null, null, null, null);

            Assert.True(actualStatus);
            Assert.Empty(actualResults);
        }

        [Fact()]
        public async Task SendEmail_ReturnNewStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsUpdatedVoidingHistory = false).Build();
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
            afterVoidEmailService.Execute(Arg.Is<VoidBetAutomationInfo>(x => x.Equals(fakeAutomationInfo)),
                Arg.Any<IEnumerable<TicketEmailModel>>(), Arg.Any<Match>(), Arg.Any<VoidedReason>(),
                Arg.Any<Message>()).Returns(fakeResults);
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, null, afterVoidEmailService, null, null);

            var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, default, default, default, default);

            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actualStatus);
            Assert.StrictEqual(fakeResults, actualResults);
        }

        [Fact()]
        public async Task SendEmail_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var (actualStatus, actualResults) = await service.SendEmail(fakeAutomationInfo, default, default, default, default);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actualStatus);
            Assert.Empty(actualResults);
        }

        [Fact()]
        public async Task UpdateEmailResult_ShouldNotCallUpdateService()
        {
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, null, null, null, null);

            await service.UpdateEmailResult(default, fakeResults);

            await voidBetAutomationService.Received(0)
                .UpdateEmailResultByVoidingHistory(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
        }

        [Fact()]
        public async Task UpdateEmailResult_ShouldCallUpdateService()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var fakeResults = Builder<Result>.CreateListOfSize(3).Random(1).With(x => x.ErrorCode = ErrorCode.SendEmailToLicenseeSuccess).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, null, null, null, null);

            await service.UpdateEmailResult(fakeAutomationInfo, fakeResults);

            await voidBetAutomationService.Received(1)
                .UpdateEmailResultByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                    Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory), Arg.Any<string>());
        }

        [Fact()]
        public async Task SendLetstalk_ReturnOldStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentLetsTalk = true).Build();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var (actualStatus, actualResults) = await service.SendLetstalk(fakeAutomationInfo, default, default, default);

            Assert.True(actualStatus);
            Assert.Empty(actualResults);
        }

        [Fact()]
        public async Task SendLetstalk_ReturnNewStatus()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().With(x => x.IsSentLetsTalk = false).Build();
            var fakeEmails = Builder<TicketEmailModel>.CreateListOfSize(2).Build();
            var fakeVoidData = Builder<VoidDataXmlModel>.CreateNew().Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var notificationLetstalkService = Substitute.For<NotificationLetstalkService>();
            notificationLetstalkService.Send(Arg.Any<NotificationLetstalkModel>(), Arg.Any<IEnumerable<TicketEmailModel>>()).Returns(false);
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, null, null, notificationLetstalkService, null);

            var (actualStatus, actualResults) = await service.SendLetstalk(fakeAutomationInfo, fakeEmails, default, fakeVoidData);

            notificationLetstalkService.Received(1)
                .Send(Arg.Any<NotificationLetstalkModel>(), Arg.Is<IEnumerable<TicketEmailModel>>(x => x.Equals(fakeEmails)));
            await voidBetAutomationService.Received(1).UpdateAfterVoidStep(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<VoidBetAutomationInfo>());
            Assert.True(actualStatus);
            Assert.Single(actualResults);
            Assert.Contains(actualResults, x => x.ErrorCode == ErrorCode.SendLetstalkFail);
        }

        [Fact()]
        public async Task SendLetstalk_ThrowException()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var loggingService = Substitute.For<LoggingService>();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new HandleAfterVoidStepServiceImp(voidBetAutomationService, null, notificationSignalRService, null, null, loggingService);

            var (actualStatus, actualResults) = await service.SendLetstalk(fakeAutomationInfo, default, default, default);

            loggingService.Received(1).Error(Arg.Any<string>(), Arg.Any<Exception>());
            await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId),
                Arg.Is<int>(x => x == fakeAutomationInfo.VoidingHistory),
                Arg.Is<int>(x => x == ExecuteStatus.WinServiceFailed));
            await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(Arg.Is<int>(x => x == fakeAutomationInfo.RequestId));
            Assert.False(actualStatus);
            Assert.Empty(actualResults);
        }

        [Fact()]
        public void WriteLogAfterVoid_ShouldCallLoggingService()
        {
            var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew().Build();
            var fakeResults = Builder<Result>.CreateListOfSize(3).Build();
            var loggingService = Substitute.For<LoggingService>();
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, loggingService);

            service.WriteLogAfterVoid(fakeResults, fakeAutomationInfo);

            loggingService.Received(1)
                .Info(Arg.Any<string>());
        }

        [Fact()]
        public void CreateResultList_ReturnExpectedResults()
        {
            var fakeNegativeBalanceUsers = "abc,xxx";
            var service = new HandleAfterVoidStepServiceImp(null, null, null, null, null, null);

            var actual = service.CreateResultList(null, null, fakeNegativeBalanceUsers, 1, 1);

            Assert.Equal(2, actual.Count());
            Assert.Contains(actual, x => x.ErrorCode == ErrorCode.NegativeBalanceUsers);
            Assert.Contains(actual, x => x.ErrorCode == ErrorCode.VoidRequestSuccess);
        }
    }
}