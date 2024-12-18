using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class UpdateTicketVoidingHistoryStageTests
{
    private VoidBetAutomationService voidBetAutomationService;
    private NotificationSignalRService notificationSignalRService;
    private LoggingService loggingService;
    public UpdateTicketVoidingHistoryStage service;

    internal void InItService()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
    }

    [Fact]
    public async Task Execute_HadIsUpdatedVoidingHistory_ReturnTrue()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsUpdatedVoidingHistory = true
        };
        var parameter = new UpdateTicketVoidingHistoryStageParameter { AutomationInfo = automationInfo };
        InItService();
        service = new UpdateTicketVoidingHistoryStageImp(null, null, null);

        // Act
        var actual = await service.Execute(parameter);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_IssueTypeIsAbnormalAccount_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { TransId = 1, WinLostDate = DateTimeOffset.Now.DateTime},
            new() { TransId  = 2, WinLostDate = DateTimeOffset.Now.DateTime}
        };
        var requestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1 },
            Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalAccount }
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsUpdatedVoidingHistory = false,
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var parameter = new UpdateTicketVoidingHistoryStageParameter
        {
            AutomationInfo = automationInfo,
            EmailTickets = emailTickets,
            NormalMessageId = 1,
            SpecialMessageId = 2
        };
        InItService();
        service = new UpdateTicketVoidingHistoryStageImp(voidBetAutomationService, null, null);

        // Act
        var actual = await service.Execute(parameter);

        // Assert
        await voidBetAutomationService.Received(1)
            .UpdateTicketVoidingHistory(parameter.AutomationInfo.VoidingHistoryId, parameter.VoidReasonId, parameter.NormalMessageId, parameter.SpecialMessageId);
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_IssueTypeIsAbnormalBet_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { TransId = 1, WinLostDate = DateTimeOffset.Now.DateTime},
            new() { TransId  = 2, WinLostDate = DateTimeOffset.Now.DateTime}
        };
        var requestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1 },
            Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet }
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsUpdatedVoidingHistory = false,
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var parameter = new UpdateTicketVoidingHistoryStageParameter
        {
            AutomationInfo = automationInfo,
            EmailTickets = emailTickets,
            NormalMessageId = 1,
            SpecialMessageId = 2
        };
        InItService();
        service = new UpdateTicketVoidingHistoryStageImp(voidBetAutomationService, null, null);

        // Act
        var actual = await service.Execute(parameter);

        // Assert
        await voidBetAutomationService.Received(1).UpdateTicketVoidingHistory(parameter.AutomationInfo.VoidingHistoryId, parameter.VoidReasonId,
            parameter.NormalMessageId, parameter.SpecialMessageId);
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { TransId = 1, WinLostDate = DateTimeOffset.Now.DateTime},
            new() { TransId  = 2, WinLostDate = DateTimeOffset.Now.DateTime}
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsUpdatedVoidingHistory = false,
            RequestInfo = null,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1,
            BroadcastSessionId = "1"
        };
        var parameter = new UpdateTicketVoidingHistoryStageParameter
        {
            AutomationInfo = automationInfo,
            EmailTickets = emailTickets,
            NormalMessageId = 1,
            SpecialMessageId = 2
        };
        InItService();
        voidBetAutomationService.When(x => x.UpdateTicketVoidingHistory(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<int?>()))
            .Do(x => x.Throws(new Exception()));
        const string error = "ERROR: UpdateTicketVoidingHistoryStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        service = new UpdateTicketVoidingHistoryStageImp(voidBetAutomationService, notificationSignalRService, loggingService);

        // Act
        var actual = await service.Execute(parameter);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        Assert.False(actual);
    }
}