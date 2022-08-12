using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.Main.Tests
{
    public class RequestAutoVoidServiceImpTests
    {
        [Fact()]
        public async Task Execute_ShouldCallCreateMessageAllRequestsStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsNullForAnyArgs();
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().CreateVoidedReason(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldNotRunMainStep()
        {
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).With(x => x.StatusId = ExecuteStatus.WinServiceDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().CreateVoidedReason(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunCreateVoidedReasonStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsNullForAnyArgs();
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendBatchMessage(default, default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunSendBatchMessageStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(false);
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunSendMessageToAdminSiteStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(false);
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunInsertVoidedTransactionStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(false);
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().UpdateTicketVoidingHistory(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunUpdateTicketVoidingHistoryStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(false);
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().SendEmail(default, default, default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunSendEmailStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((false, null));
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().UpdateEmailResult(default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunSendLetstalkStep()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((true, null));
            handleAfterVoidStepService.SendLetstalk(default, default, default, default).ReturnsForAnyArgs((false, null));
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, default);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendLetstalk(default, default, default, default);
            await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateServiceStatusByVoidingHistory(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunAllSteps()
        {
            var fakeMatch = Builder<Match>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeMessage = Builder<Message>.CreateNew().Build();
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.RequestInfo = new RequestDetailViewModel()
                {
                    Request = new RequestModel() { IssueTypeId = IssueTypeConstant.DangerAccepted },
                    EventByMatch = new RequestEventModel() { FilterBy = 0 }
                }).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<HandleAfterVoidStepService>();
            handleAfterVoidStepService
                .CreateMessageIdForAllRequests(default, default, default, default, default, default)
                .ReturnsForAnyArgs(fakeMessage);
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((true, null));
            handleAfterVoidStepService.SendLetstalk(default, default, default, default).ReturnsForAnyArgs((true, null));
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestAutoVoidServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendBatchMessage(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendMessageToAdminSite(default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendEmail(default, default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).SendLetstalk(default, default, default, default);
            await voidBetAutomationService.ReceivedWithAnyArgs(3).UpdateServiceStatusByVoidingHistory(default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3).UpdateCompletedStatusForRequests(default, default);
            handleAfterVoidStepService.ReceivedWithAnyArgs(3).WriteLogAfterVoid(default, default);
        }
    }
}