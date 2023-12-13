using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using FizzWare.NBuilder;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class RequestAutoVoidServiceImpTests
{
    private AfterVoidProcessStage afterVoidProcessStage;
    private NotificationSignalRService notificationSignalRService;
    private VoidBetAutomationService voidBetAutomationService;

    public void InitFakeServiceTests()
    {
        afterVoidProcessStage = Substitute.For<AfterVoidProcessStage>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
    }

    [Fact]
    public async Task Execute_ShouldCallCreateMessageAllRequestsStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsNullForAnyArgs();
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, true, RequestStatusConstant.Completed);

        // Assert
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().CreateVoidedReason(default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldNotRunMainStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).With(x => x.StatusId = ExecuteStatus.Done).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().CreateVoidedReason(default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunCreateVoidedReasonStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsNullForAnyArgs();
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, true, RequestStatusConstant.Completed);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().SendBatchMessage(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunSendBatchMessageStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(false);
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs()
            .InsertVoidedTransaction(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunSendMessageToAdminSiteStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(false);
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs()
            .InsertVoidedTransaction(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunInsertVoidedTransactionStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(false);
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).InsertVoidedTransaction(default, default, default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs()
            .UpdateTicketVoidingHistory(default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunUpdateTicketVoidingHistoryStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(false);
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, default, default);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).InsertVoidedTransaction(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1)
            .UpdateTicketVoidingHistory(default, default, default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().SendEmail(default, default, default, default, default);
    }

    [Fact]
    public async Task Execute_ShouldRunSendEmailStep()
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 }
            }).Build();
        fakeAutomationInfo.MatchId = 0;
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((false, null));
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage, default);

        // Act
        await service.Execute(fakeAutomationInfo, true, RequestStatusConstant.Completed);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).InsertVoidedTransaction(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1)
            .UpdateTicketVoidingHistory(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendEmail(default, default, default, default, default);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().UpdateEmailResult(default, default);
    }

    [Theory]
    [InlineData(true, RequestStatusConstant.Completed)]
    [InlineData(false, RequestStatusConstant.Failed)]
    public async Task Execute_ShouldRunAllSteps(bool isLatestInRequest, string requestStatus)
    {
        // Arrange
        InitFakeServiceTests();
        var fakeMatch = Builder<Match>.CreateNew().Build();
        var fakeMessage = Builder<Message>.CreateNew().Build();
        var fakeVoidedReason = Builder<VoidedReason>.CreateNew().Build();
        var fakeAutomationInfo = Builder<VoidBetAutomationInfo>.CreateNew()
            .With(x => x.RequestInfo = new RequestDetailViewModel
            {
                Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted },
                EventByMatch = new RequestEventModel { FilterBy = 0 },
            }).Build();
        fakeAutomationInfo.MatchId = 1;
        voidBetAutomationService.GetMatchInfo(default).ReturnsForAnyArgs(fakeMatch);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(default).ReturnsForAnyArgs(new List<VoidDataXmlModel>());
        afterVoidProcessStage
            .CreateMessageIdForAutoVoid(default, default, default, default)
            .ReturnsForAnyArgs(fakeMessage);
        afterVoidProcessStage.CreateVoidedReason(default, default, default).ReturnsForAnyArgs(fakeVoidedReason);
        afterVoidProcessStage.SendBatchMessage(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.InsertVoidedTransaction(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.UpdateTicketVoidingHistory(default, default, default, default).ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendEmail(default, default, default, default, default).ReturnsForAnyArgs((true, null));
        var service = new RequestAutoVoidServiceImp(voidBetAutomationService, afterVoidProcessStage,
            notificationSignalRService);

        // Act
        await service.Execute(fakeAutomationInfo, isLatestInRequest, requestStatus);

        // Assert
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).CreateVoidedReason(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendBatchMessage(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendMessageToAdminSite(default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).InsertVoidedTransaction(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1)
            .UpdateTicketVoidingHistory(default, default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).SendEmail(default, default, default, default, default);
        await voidBetAutomationService.ReceivedWithAnyArgs(1)
            .UpdateServiceStatusByVoidingHistory(default, default, default);
        await afterVoidProcessStage.ReceivedWithAnyArgs(1).WriteLogAfterVoid(default, default);
    }
}