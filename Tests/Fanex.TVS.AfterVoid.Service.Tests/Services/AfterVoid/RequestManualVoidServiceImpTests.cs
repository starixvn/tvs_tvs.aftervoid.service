using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.Tests
{
    public class RequestManualVoidServiceImpTests
    {
        [Fact]
        public async Task Execute_ShouldNotRunMainSteps()
        {
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.Done).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, default, default);

            await service.Execute(fakeAutomationInfoList);

            await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetVoidedTicketsForSendEmail(default);
        }

        [Fact]
        public async Task Execute_ShouldRunCreateMessageIdForManualVoidStep()
        {
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsNullForAnyArgs();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().CreateVoidedReason(default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunCreateVoidedReasonStep()
        {
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsNullForAnyArgs();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendBatchMessage(default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunSendBatchMessageStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunSendMessageToAdminSiteStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunInsertVoidedTransactionStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().UpdateTicketVoidingHistory(default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunUpdateTicketVoidingHistoryStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendEmail(default, default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunSendEmailStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((false, null));
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendLetstalk(default, default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunSendLetstalkStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((true, null));
            handleAfterVoidStepService.SendLetstalk(default, default, default, default).ReturnsForAnyArgs((false, null));
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendLetstalk(default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await voidBetAutomationService.DidNotReceiveWithAnyArgs()
                .UpdateServiceStatusByVoidingHistory(default, default, default);
        }

        [Fact]
        public async Task Execute_ShouldRunAllSteps()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.Running).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateMessageIdForManualVoid(default, default).ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((true, null));
            handleAfterVoidStepService.SendLetstalk(default, default, default, default).ReturnsForAnyArgs((true, null));
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestManualVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateMessageIdForManualVoid(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendLetstalk(default, default, default, default);
            await voidBetAutomationService.ReceivedWithAnyArgs(3).UpdateServiceStatusByVoidingHistory(default, default, default);
            handleAfterVoidStepService.ReceivedWithAnyArgs(3).WriteLogAfterVoid(default, default);
        }
    }
}