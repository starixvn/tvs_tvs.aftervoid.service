using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecoverStage;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage.AutoRecoverStage;

public class SendEmailRecoverStageTests
{
    [Fact]
    public async Task Execute_HadSentEmail_ReturnTrue()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = true
        };
        var expectedResults = new List<Result>();
        var service = new SendEmailRecoverStageImp(null, null, null, null);

        // Act
        var (actual, results) = await service.Execute(automationInfo, null);

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
        var tickets = new List<TicketEmailModel> { new() };
        var expectedResults = new List<Result>();
        var tvsWebSiteHttpClientService = Substitute.For<TVSWebSiteHttpClientService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        tvsWebSiteHttpClientService.SendEmailRecover(Arg.Any<SendEmailRecoverApiModel>()).ReturnsForAnyArgs((true, expectedResults));

        var service = new SendEmailRecoverStageImp(voidBetAutomationService, null, null, tvsWebSiteHttpClientService);

        // Act
        var (actual, results) = await service.Execute(automationInfo, tickets);

        // Assert
        await tvsWebSiteHttpClientService.Received(1).SendEmailRecover(Arg.Any<SendEmailRecoverApiModel>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
        AssertExtension.Equal(expectedResults, results);
    }

    [Fact]
    public async Task Execute_HasExceptionSendEmailRecover_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = false,
            VoidingHistoryId = 1,
            RequestId = 1,
            BroadcastSessionId = "1"
        };
        const string error = "ERROR: SendEmailRecoverStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        var tickets = new List<TicketEmailModel> { new() };
        var expectedResults = new List<Result>();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();

        var service = new SendEmailRecoverStageImp(voidBetAutomationService, notificationSignalRService, loggingService, null);

        // Act
        var (actual, results) = await service.Execute(automationInfo, tickets);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        Assert.False(actual);
        AssertExtension.Equal(expectedResults, results);
    }

    [Fact]
    public async Task Execute_NotSentEmail_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = false,
            VoidingHistoryId = 1,
            RequestId = 1,
            BroadcastSessionId = "1"
        };
        const string error = "ERROR: SendEmailRecoverStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        var tickets = new List<TicketEmailModel> { new() };
        var expectedResults = new List<Result>();
        var tvsWebSiteHttpClientService = Substitute.For<TVSWebSiteHttpClientService>();
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        tvsWebSiteHttpClientService.SendEmailRecover(Arg.Any<SendEmailRecoverApiModel>()).ReturnsForAnyArgs((false, expectedResults));

        var service = new SendEmailRecoverStageImp(voidBetAutomationService, notificationSignalRService, loggingService, tvsWebSiteHttpClientService);

        // Act
        var (actual, results) = await service.Execute(automationInfo, tickets);

        // Assert
        await tvsWebSiteHttpClientService.Received(1).SendEmailRecover(Arg.Any<SendEmailRecoverApiModel>());
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        Assert.False(actual);
        AssertExtension.Equal(expectedResults, results);
    }
}