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

public class InsertVoidedTransactionStageTests
{
    private VoidBetAutomationService voidBetAutomationService;
    private SendMessageService sendMessageService;
    private SignalRService notificationSignalRService;
    private LoggingService loggingService;
    public InsertVoidedTransactionStage Service;

    internal void InItService()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<SignalRService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService = Substitute.For<SendMessageService>();
    }

    [Fact]
    public async Task Execute_HadInsertedVoidedTransaction_ReturnTrue()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsInsertedVoidedTransaction = true
        };
        var parameter = new InsertVoidedTransactionStageParameter
        {
            AutomationInfo = automationInfo
        };
        InItService();
        Service = new InsertVoidedTransactionStageImp(null, null, null, null);

        // Act
        var actual = await Service.Execute(parameter);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasOnlyNormalTickets_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCancelCashOut = true},
            new() { IsCancelCashOut = false}
        };
        var requestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1 },
            Request = new RequestModel { IssueTypeId = 1 }
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsInsertedVoidedTransaction = false,
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var parameter = new InsertVoidedTransactionStageParameter
        {
            AutomationInfo = automationInfo,
            NormalMessageId = 1,
            VoidReasonId = 1,
            NormalTickets = emailTickets,
            SpecialTickets = new List<TicketEmailModel>()
        };

        InItService();
        Service = new InsertVoidedTransactionStageImp(voidBetAutomationService, sendMessageService, null, null);

        // Act
        var actual = await Service.Execute(parameter);

        // Assert
        await sendMessageService.Received(1).InsertVoidedTransaction(Arg.Any<IEnumerable<TicketEmailModel>>(), Arg.Any<int>(), 1, 1, 1, 1);
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasNormalAndSpecialTickets_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCancelCashOut = true},
            new() { IsCancelCashOut = false}
        };
        var requestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1 },
            Request = new RequestModel { IssueTypeId = 1 }
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsInsertedVoidedTransaction = false,
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var parameter = new InsertVoidedTransactionStageParameter
        {
            AutomationInfo = automationInfo,
            NormalMessageId = 1,
            SpecialMessageId = 2,
            SpecialTickets = emailTickets,
            VoidReasonId = 1,
            NormalTickets = emailTickets
        };

        InItService();
        Service = new InsertVoidedTransactionStageImp(voidBetAutomationService, sendMessageService, null, null);

        // Act
        var actual = await Service.Execute(parameter);

        // Assert
        await sendMessageService.Received(2).InsertVoidedTransaction(Arg.Any<IEnumerable<TicketEmailModel>>(), Arg.Any<int>(), 1, 1, 1, 1);
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
        Assert.True(actual);
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCancelCashOut = true},
            new() { IsCancelCashOut = false}
        };
        var requestInfo = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel { SubIssueTypeId = 1 },
            Request = new RequestModel { IssueTypeId = 1 }
        };

        var automationInfo = new VoidBetAutomationInfo
        {
            IsInsertedVoidedTransaction = false,
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1,
            VoidingHistoryId = 1,
            BroadcastSessionId = "1"
        };
        var parameter = new InsertVoidedTransactionStageParameter
        {
            AutomationInfo = automationInfo,
            NormalMessageId = 1,
            VoidReasonId = 1,
            NormalTickets = emailTickets,
            SpecialTickets = null
        };
        const string error = "ERROR: InsertVoidedTransactionStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        InItService();
        Service = new InsertVoidedTransactionStageImp(voidBetAutomationService, null, notificationSignalRService, loggingService);

        // Act
        _ = await Service.Execute(parameter);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(1, 1, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(1);
    }
}