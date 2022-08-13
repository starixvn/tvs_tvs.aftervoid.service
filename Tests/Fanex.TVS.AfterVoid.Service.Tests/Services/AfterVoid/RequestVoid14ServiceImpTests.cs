using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.Tests
{
    public class RequestVoid14ServiceImpTests
    {
        [Fact()]
        public async Task Execute_ShouldNotRunMainSteps()
        {
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.WinServiceDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            var service = new RequestVoid14ServiceImp(voidBetAutomationService, default, default);

            await service.Execute(fakeAutomationInfoList);

            await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetVoidedTicketsForSendEmail(default);
        }

        [Fact()]
        public async Task Execute_ShouldRunCreateVoidedReasonStep()
        {
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.DBJobDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsNullForAnyArgs();
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunInsertVoidedTransactionStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.DBJobDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().UpdateTicketVoidingHistory(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunUpdateTicketVoidingHistoryStep()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.DBJobDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(false);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await notificationSignalRService.ReceivedWithAnyArgs(3)
                .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
            await voidBetAutomationService.DidNotReceiveWithAnyArgs().UpdateServiceStatusByVoidingHistory(default, default, default);
        }

        [Fact()]
        public async Task Execute_ShouldRunAllSteps()
        {
            var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
            var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
            var fakeAutomationInfoList = Builder<VoidBetAutomationInfo>.CreateListOfSize(3)
                .All().With(x => x.StatusId = ExecuteStatus.DBJobDone).Build();
            var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
            voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
            var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
            handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
            handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
            handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default).ReturnsForAnyArgs(true);
            var notificationSignalRService = Substitute.For<NotificationSignalRService>();
            var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

            await service.Execute(fakeAutomationInfoList);

            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).CreateVoidedReason(default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).InsertVoidedTransaction(default, default, default, default);
            await handleAfterVoidStepService.ReceivedWithAnyArgs(3).UpdateTicketVoidingHistory(default, default, default);
            await voidBetAutomationService.ReceivedWithAnyArgs(3).UpdateServiceStatusByVoidingHistory(default, default, default);
            handleAfterVoidStepService.ReceivedWithAnyArgs(3).WriteLogAfterVoid(default, default);
        }
    }
}