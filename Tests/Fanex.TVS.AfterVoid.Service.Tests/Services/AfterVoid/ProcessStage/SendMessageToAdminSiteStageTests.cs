using System;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class SendMessageToAdminSiteStageTests
{
    private LoggingService loggingService;
    private SignalRService notificationSignalRService;
    private SendMessageService sendMessageService;
    private VoidBetAutomationService voidBetAutomationService;
    public SendMessageToAdminSiteStage Service;

    internal void InItService()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<SignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService = Substitute.For<SendMessageService>();
    }

    [Fact]
    public async Task Execute_HadSentMessageToAdminSite_ReturnTrue()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentMessageToAdminSite = true
        };

        InItService();
        Service = new SendMessageToAdminSiteStageImp(null, null, null, null);

        // Act
        var actual = await Service.Execute(automationInfo, null);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasSentMessageToAdminSite_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentMessageToAdminSite = false,
            VoidingHistoryId = 1,
            RequestId = 1
        };
        InItService();
        Service = new SendMessageToAdminSiteStageImp(voidBetAutomationService, sendMessageService, null, null);

        // Act
        var actual = await Service.Execute(automationInfo, new Message());

        // Assert
        await sendMessageService.Received(1).InsertMessageIntoAdminSite(Arg.Any<Message>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentMessageToAdminSite = false,
            VoidingHistoryId = 1,
            RequestId = 1,
            BroadcastSessionId = "1"
        };

        const string error = "ERROR: SendMessageToAdminSiteStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        InItService();
        Service = new SendMessageToAdminSiteStageImp(voidBetAutomationService, null, notificationSignalRService, loggingService);

        // Act
        var actual = await Service.Execute(automationInfo, new Message());

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        Assert.False(actual);
    }
}