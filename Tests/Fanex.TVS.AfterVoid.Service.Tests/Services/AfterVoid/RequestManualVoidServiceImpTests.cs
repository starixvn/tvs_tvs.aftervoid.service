using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class RequestManualVoidServiceImpTests
{
    private readonly AfterVoidProcessStage afterVoidProcessStage;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly RequestManualVoidServiceImp service;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public RequestManualVoidServiceImpTests()
    {
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        afterVoidProcessStage = Substitute.For<AfterVoidProcessStage>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        service = new RequestManualVoidServiceImp(voidBetAutomationService, afterVoidProcessStage,
            notificationSignalRService);
    }

    [Fact]
    public async Task Execute_ShouldNotRunMainSteps()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 1, StatusId = ExecuteStatus.Done };

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await voidBetAutomationService.DidNotReceiveWithAnyArgs()
            .GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
    }

    [Fact]
    public async Task Execute_NotHaveMessage_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 1, StatusId = ExecuteStatus.Running };
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs((Message)null);

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        await afterVoidProcessStage.Received(1)
            .CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>());
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_NotHaveVoidedReason_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 1, StatusId = ExecuteStatus.Running };
        var message = new Message { MessageId = 1 };
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs((VoidedReason)null);

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1).CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>());
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_NotSentBatchMessage_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
            { VoidingHistoryId = 1, SiteDbGroupId = 1, StatusId = ExecuteStatus.Running };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(false);

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1).SendBatchMessage(automationInfo, voidedTickets, message.MessageId,
            Arg.Any<VoidDataXmlModel>());
        await voidBetAutomationService.Received(1)
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_IsNotSentMessageToAdminSite_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1, SiteDbGroupId = 1, StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } }
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(false);

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1).SendMessageToAdminSite(automationInfo, message);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_IsNotInsertedVoidedTransaction_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1,
            SiteDbGroupId = 1,
            StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } }
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId)
            .ReturnsForAnyArgs(false);

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1)
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_IsNotUpdatedVoidingHistory_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1,
            SiteDbGroupId = 1,
            StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } }
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(false);
        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1)
            .UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
    }

    [Fact]
    public async Task Execute_IsNotSentEmail_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1,
            SiteDbGroupId = 1,
            StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } },
            MatchId = 1
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        var matchInfo = new Match();
        var currentEmailResults = new List<Result>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetMatchInfo(automationInfo.MatchId).ReturnsForAnyArgs(matchInfo);
        afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message)
            .ReturnsForAnyArgs((false, currentEmailResults));

        // Act
        await service.Execute(automationInfo, default, default, default);

        // Assert
        await afterVoidProcessStage.Received(1)
            .SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message);
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("1");
        await voidBetAutomationService.Received(1).GetMatchInfo(automationInfo.MatchId);
    }

    [Theory]
    [InlineData(false, RequestStatusConstant.Running)]
    [InlineData(true, RequestStatusConstant.Failed)]
    public async Task Execute_IsNotLastedInRequestOrCurrentRequestStatusIsFailed_CallExpected(bool isLastedInRequest,
        string currentRequestStatus)
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1,
            SiteDbGroupId = 1,
            StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } },
            MatchId = 0
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        var matchInfo = new Match();
        var currentEmailResults = new List<Result>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetMatchInfo(automationInfo.MatchId).ReturnsForAnyArgs(matchInfo);
        afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message)
            .ReturnsForAnyArgs((true, currentEmailResults));

        // Act
        await service.Execute(automationInfo, isLastedInRequest, currentRequestStatus, default);

        // Assert
        await afterVoidProcessStage.Received(1).UpdateEmailResult(automationInfo, currentEmailResults);
        await afterVoidProcessStage.Received(1).CallCashOutApi(automationInfo, voidedTickets);
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await notificationSignalRService.DidNotReceiveWithAnyArgs()
            .UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
        await afterVoidProcessStage.DidNotReceiveWithAnyArgs().NotifyVoidResultsSignalR(automationInfo,
            currentEmailResults, "1", Arg.Any<IEnumerable<EmailResultModel>>());
        await afterVoidProcessStage.Received(1).WriteLogAfterVoid(currentEmailResults, automationInfo);
    }

    [Fact]
    public async Task Execute_IsLastedInRequestOrCurrentRequestStatusIsNotFailed_CallExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoidTimes = new List<VoidDataXmlModel> { new() { CreatorName = "1" } };
        var automationInfo = new VoidBetAutomationInfo
        {
            VoidingHistoryId = 1,
            SiteDbGroupId = 1,
            StatusId = ExecuteStatus.Running,
            RequestInfo = new RequestDetailViewModel
                { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } },
            MatchId = 0
        };
        var message = new Message { MessageId = 1 };
        var voidedReason = new VoidedReason { ReasonId = 1 };
        var voidedTickets = new List<TicketEmailModel>();
        var matchInfo = new Match();
        var currentEmailResults = new List<Result>();
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId)
            .ReturnsForAnyArgs(allVoidDataInCurrentVoidTimes);
        afterVoidProcessStage.CreateMessageIdForManualVoid(automationInfo, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(message);
        afterVoidProcessStage.CreateVoidedReason(automationInfo, 0, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(voidedReason);
        voidBetAutomationService
            .GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true)
            .ReturnsForAnyArgs(voidedTickets);
        afterVoidProcessStage
            .SendBatchMessage(automationInfo, voidedTickets, message.MessageId, Arg.Any<VoidDataXmlModel>())
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage.SendMessageToAdminSite(automationInfo, message).ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .InsertVoidedTransaction(automationInfo, voidedTickets, message.MessageId, voidedReason.ReasonId)
            .ReturnsForAnyArgs(true);
        afterVoidProcessStage
            .UpdateTicketVoidingHistory(automationInfo, message.MessageId, voidedReason.ReasonId, voidedTickets)
            .ReturnsForAnyArgs(true);
        voidBetAutomationService.GetMatchInfo(automationInfo.MatchId).ReturnsForAnyArgs(matchInfo);
        afterVoidProcessStage.SendEmail(automationInfo, voidedTickets, matchInfo, voidedReason, message)
            .ReturnsForAnyArgs((true, currentEmailResults));

        // Act
        await service.Execute(automationInfo, true, RequestStatusConstant.Running, default);

        // Assert
        await notificationSignalRService.Received(1)
            .UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
        await afterVoidProcessStage.Received(1).NotifyVoidResultsSignalR(automationInfo, currentEmailResults, "1",
            Arg.Any<IEnumerable<EmailResultModel>>());
    }
}