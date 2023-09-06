using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using FizzWare.NBuilder;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class RequestVoid14ServiceImpTests
{
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly RequestVoid14ServiceImp service;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public RequestVoid14ServiceImpTests()
    {
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        afterVoidProcessStage = Substitute.For<AfterVoidProcessStage>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        service = new RequestVoid14ServiceImp(voidBetAutomationService, afterVoidProcessStage,
            notificationSignalRService);
    }

    [Fact]
    public async Task Execute_ShouldNotRunMainSteps()
    {
        // Arrange
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.StatusId = ExecuteStatus.Done).Build();

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await voidBetAutomationService.DidNotReceiveWithAnyArgs()
            .GetVoidedTicketsByVoidingHistory(default, default, true);
    }

    [Fact]
    public async Task Execute_HasVoidedReasonIsNull_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo { VoidingHistoryId = 1, SiteDbGroupId = 0 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs((VoidedReason)null);

        // Act
        await service.Execute(automationInfo, true, "");

        // Assert
        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(1, 0, true);
        await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(1);
        await afterVoidProcessStage.Received(1).CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>());
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("123");
    }

    [Fact]
    public async Task Execute_HasReasonIdIs0_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo { VoidingHistoryId = 1, SiteDbGroupId = 0 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        var voidedReason = new VoidedReason { ReasonId = 0 };
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);

        // Act
        await service.Execute(automationInfo, true, "");

        // Assert
        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(1, 0, true);
        await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(1);
        await afterVoidProcessStage.Received(1).CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>());
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("123");
    }

    [Fact]
    public async Task Execute_IsNotInsertedVoidedTransaction_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo { VoidingHistoryId = 1, SiteDbGroupId = 0 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId)
            .ReturnsForAnyArgs(false);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);

        // Act
        await service.Execute(automationInfo, true, "");

        // Assert
        await afterVoidProcessStage.Received(1)
            .InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("123");
    }

    [Fact]
    public async Task Execute_IsNotUpdatedVoidingHistory_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo { VoidingHistoryId = 1, SiteDbGroupId = 0 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(false);

        // Act
        await service.Execute(automationInfo, true, "");

        // Assert
        await afterVoidProcessStage.Received(1)
            .UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("123");
    }

    [Theory]
    [InlineData(false, RequestStatusConstant.Running)]
    [InlineData(true, RequestStatusConstant.Failed)]
    public async Task Execute_RequestStatusOrLastedInRequestIsFailed_CalledExpected(bool isLastedInRequest,
        string currentRequestStatus)
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 0, RequestId = 1, MatchId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(true);

        // Act
        await service.Execute(automationInfo, isLastedInRequest, currentRequestStatus);

        // Assert
        await afterVoidProcessStage.Received(1).CallCashOutApi(automationInfo, voidedTickets);
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await afterVoidProcessStage.Received(1).WriteLogAfterVoid(Arg.Any<List<Result>>(), automationInfo);
        await notificationSignalRService.DidNotReceiveWithAnyArgs()
            .UpdateCompletedStatusForRequests(Arg.Any<int>(), Arg.Any<int>());
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().NotifyVoidResultsSignalR(
            Arg.Any<VoidBetAutomationInfo>(), Arg.Any<List<Result>>(), default, Arg.Any<List<EmailResultModel>>());
    }

    [Fact]
    public async Task Execute_RequestStatusIsTrueOrLastedInRequestIsRunning_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 0, RequestId = 1, MatchId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "123" } };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        afterVoidProcessStage.InsertVoidedTransaction(automationInfo, voidedTickets, 0, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(1, 0, true).ReturnsForAnyArgs(voidedTickets);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(1)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        afterVoidProcessStage.UpdateTicketVoidingHistory(automationInfo, 0, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(true);

        // Act
        await service.Execute(automationInfo, true, RequestStatusConstant.Running);

        // Assert
        await afterVoidProcessStage.Received(1).WriteLogAfterVoid(Arg.Any<List<Result>>(), automationInfo);
        await notificationSignalRService.Received(1).UpdateCompletedStatusForRequests(Arg.Any<int>(), Arg.Any<int>());
        await afterVoidProcessStage.Received(1).NotifyVoidResultsSignalR(Arg.Any<VoidBetAutomationInfo>(),
            Arg.Any<List<Result>>(), "123", Arg.Any<List<EmailResultModel>>());
    }
}