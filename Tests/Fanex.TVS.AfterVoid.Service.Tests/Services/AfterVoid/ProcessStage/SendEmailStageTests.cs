using System;
using System.Collections.Generic;
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

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class SendEmailStageTests
{
    private LoggingService loggingService;
    private NotificationSignalRService notificationSignalRService;
    private VoidBetAutomationService voidBetAutomationService;
    private AfterVoidEmailService afterVoidEmailService;
    public SendEmailStage Service;

    internal void InItService()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        afterVoidEmailService = Substitute.For<AfterVoidEmailService>();
    }

    [Fact]
    public async Task Execute_HadSentEmail_ReturnTrue()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = true
        };
        var parameter = new SendEmailStageParameter
        {
            AutomationInfo = automationInfo
        };
        var expectedResults = new List<Result>();
        InItService();
        Service = new SendEmailStageImp(null, null, null, null);

        // Act
        var (actual, results) = await Service.Execute(parameter);

        // Assert
        Assert.True(actual);
        AssertExtension.Equal(expectedResults, results);
    }

    [Fact]
    public async Task Execute_HasSendEmailStage_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = false,
            VoidingHistoryId = 1,
            RequestId = 1
        };
        var parameter = new SendEmailStageParameter
        {
            AutomationInfo = automationInfo
        };
        var expectedResults = new List<Result>();
        InItService();
        afterVoidEmailService.Execute(Arg.Any<SendEmailStageParameter>()).ReturnsForAnyArgs(expectedResults);
        Service = new SendEmailStageImp(voidBetAutomationService, null, null, afterVoidEmailService);

        // Act
        var (actual, results) = await Service.Execute(parameter);

        // Assert
        await afterVoidEmailService.Received(1).Execute(parameter);
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
        AssertExtension.Equal(expectedResults, results);
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = false,
            VoidingHistoryId = 1,
            RequestId = 1,
            BroadcastSessionId = "1"
        };
        var parameter = new SendEmailStageParameter
        {
            AutomationInfo = automationInfo
        };

        const string error = "ERROR: SendEmailStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        InItService();
        Service = new SendEmailStageImp(voidBetAutomationService, notificationSignalRService, loggingService, null);

        // Act
        _ = await Service.Execute(parameter);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
    }
}