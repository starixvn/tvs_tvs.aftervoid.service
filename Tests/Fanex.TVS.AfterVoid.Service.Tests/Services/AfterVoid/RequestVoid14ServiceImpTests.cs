using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class RequestVoid14ServiceImpTests
{
    [Fact]
    public async Task Execute_ShouldNotRunMainSteps()
    {
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Done).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        var service = new RequestVoid14ServiceImp(voidBetAutomationService, default, default);

        await service.Execute(fakeAutomationInfo, default, default);

        await voidBetAutomationService.DidNotReceiveWithAnyArgs().GetVoidedTicketsByVoidingHistory(default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunCreateVoidedReasonStep()
    {
        var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Running).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
        var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
        handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsNullForAnyArgs();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

        await service.Execute(fakeAutomationInfo, default, default);

        await handleAfterVoidStepService.ReceivedWithAnyArgs().CreateVoidedReason(default, default, default);
        await notificationSignalRService.ReceivedWithAnyArgs()
            .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
        await handleAfterVoidStepService.DidNotReceiveWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunInsertVoidedTransactionStep()
    {
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Running).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
        var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
        handleAfterVoidStepService.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(false);
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

        await service.Execute(fakeAutomationInfo, default, default);

        await handleAfterVoidStepService.ReceivedWithAnyArgs().CreateVoidedReason(default, default, default);
        await handleAfterVoidStepService.ReceivedWithAnyArgs().InsertVoidedTransaction(default, default, default, default);
        await notificationSignalRService.ReceivedWithAnyArgs()
            .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
        await handleAfterVoidStepService.DidNotReceiveWithAnyArgs()
            .UpdateTicketVoidingHistory(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunUpdateTicketVoidingHistoryStep()
    {
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Running).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
        var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
        handleAfterVoidStepService.CreateVoidedReason(default, default, default)
            .ReturnsForAnyArgs(fakeVoidedReason);
        handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default)
            .ReturnsForAnyArgs(true);
        handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default)
            .ReturnsForAnyArgs(false);
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

        await service.Execute(fakeAutomationInfo, default, default);

        await handleAfterVoidStepService.ReceivedWithAnyArgs().CreateVoidedReason(default, default, default);
        await handleAfterVoidStepService.ReceivedWithAnyArgs()
            .InsertVoidedTransaction(default, default, default, default);
        await handleAfterVoidStepService.ReceivedWithAnyArgs()
            .UpdateTicketVoidingHistory(default, default, default, default);
        await notificationSignalRService.ReceivedWithAnyArgs()
            .NotifyErrorWhenExecuteAfterVoidServiceToUser(default);
        await voidBetAutomationService.DidNotReceiveWithAnyArgs()
            .UpdateServiceStatusByVoidingHistory(default, default, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Execute_ShouldRunAllSteps(bool isLastedInRequest)
    {
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeVoidDataXml = Builder<VoidDataXmlModel>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Running).Build();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(fakeVoidDataXml);
        var handleAfterVoidStepService = Substitute.For<AfterVoidProcessStage>();
        handleAfterVoidStepService.CreateVoidedReason(default, default, default)
            .ReturnsForAnyArgs(fakeVoidedReason);
        handleAfterVoidStepService.InsertVoidedTransaction(default, default, default, default)
            .ReturnsForAnyArgs(true);
        handleAfterVoidStepService.UpdateTicketVoidingHistory(default, default, default, default)
            .ReturnsForAnyArgs(true);
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var service = new RequestVoid14ServiceImp(voidBetAutomationService, handleAfterVoidStepService, notificationSignalRService);

        await service.Execute(fakeAutomationInfo, isLastedInRequest, RequestStatusConstant.Completed);

        await handleAfterVoidStepService.ReceivedWithAnyArgs().CreateVoidedReason(default, default, default);
        await handleAfterVoidStepService.ReceivedWithAnyArgs()
            .InsertVoidedTransaction(default, default, default, default);
        await handleAfterVoidStepService.ReceivedWithAnyArgs()
            .UpdateTicketVoidingHistory(default, default, default, default);
        await voidBetAutomationService.ReceivedWithAnyArgs()
            .UpdateServiceStatusByVoidingHistory(default, default, default);
        handleAfterVoidStepService.ReceivedWithAnyArgs().WriteLogAfterVoid(default, default);
    }
}