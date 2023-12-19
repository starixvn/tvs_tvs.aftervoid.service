using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.ProcessStage;

public class CreateVoidReasonStageTests
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendMessageService sendMessageService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    public CreateVoidReasonStage Service;

    public CreateVoidReasonStageTests()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService = Substitute.For<SendMessageService>();
    }

    [Fact]
    public async Task Execute_HasReasonId_HasCalledGetVoidedReason()
    {
        // Arrange
        var reason = new VoidedReason();
        var automationInfo = new VoidBetAutomationInfo
        {
            ReasonId = 1
        };
        var parameter = new CreateVoidReasonStageParameter
        {
            AutomationInfo = automationInfo
        };
        sendMessageService.GetVoidedReason(1).ReturnsForAnyArgs(reason);
        Service = new CreateVoidReasonStageImp(null, sendMessageService, null, null);

        // Act
        var actual = await Service.Execute(parameter);

        // Assert
        AssertExtension.Equal(reason, actual);
        await sendMessageService.Received(1).GetVoidedReason(1);
    }

    [Fact]
    public async Task Execute_NotHasReasonId_HasInsertReason()
    {
        // Arrange
        var reason = new VoidedReason { ReasonEnglish = "1", ReasonChineseSimplified = "1" };
        var requestInfo = new RequestDetailViewModel { EventByMatch = new RequestEventModel { SubIssueTypeId = 1 } };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsAutoVoid = true,
            RequestId = 1,
            VoidingHistoryId = 1,
            ReasonId = 0,
            RequestInfo = requestInfo
        };
        var parameter = new CreateVoidReasonStageParameter
        {
            AutomationInfo = automationInfo,
            SportTypeId = 1,
            VoidData = null,
        };
        sendMessageService.InitializeVoidReason(true, 1, 1, null).ReturnsForAnyArgs(reason);
        sendMessageService.InsertReason("1", "1").ReturnsForAnyArgs(1);
        Service = new CreateVoidReasonStageImp(voidBetAutomationService, sendMessageService, null, null);

        // Act
        _ = await Service.Execute(parameter);

        // Assert
        await sendMessageService.Received(1).InitializeVoidReason(true, 1, 1, null);
        await sendMessageService.Received(1).InsertReason("1", "1");
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            ReasonId = 1,
            BroadcastSessionId = "1",
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var parameter = new CreateVoidReasonStageParameter
        {
            AutomationInfo = automationInfo
        };
        const string error = "ERROR: CreateVoidReasonStage.Execute - SessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        Service = new CreateVoidReasonStageImp(voidBetAutomationService, null, notificationSignalRService, loggingService);

        // Act
        _ = await Service.Execute(parameter);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(1, 1, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(1);
    }
}