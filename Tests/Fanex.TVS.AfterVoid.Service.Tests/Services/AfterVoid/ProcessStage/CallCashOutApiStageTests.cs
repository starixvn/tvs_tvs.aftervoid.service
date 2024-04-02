using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class CallCashOutApiStageTests
{
    private LoggingService loggingService;
    private CashOutHttpClientService cashOutHttpClientService;
    private VoidBetAutomationService voidBetAutomationService;
    public CallCashOutApiStage Service;

    internal void InItService()
    {
        loggingService = Substitute.For<LoggingService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        cashOutHttpClientService = Substitute.For<CashOutHttpClientService>();
    }

    [Fact]
    public async Task ExecuteForVoid_CallCashOutApiSuccess_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForVoid(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(1).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task ExecuteForVoid_HasException_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            BroadcastSessionId = "1"
        };
        const string error = "ERROR: CallCashOutApiStage.ExecuteForVoid - SessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        InItService();
        Service = new CallCashOutApiStageImp(null, null, loggingService);

        // Act
        await Service.ExecuteForVoid(automationInfo, emailTickets);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
    }

    [Fact]
    public async Task ExecuteForVoid_IsRunExternalService_NotCallCashOutApi()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = true,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForVoid(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task ExecuteForVoid_NoHasTicketCashOut_NotCallCashOutApi()
    {
        // Arrange
        var emailTickets = new List<TicketEmailModel>
        {
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForVoid(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task ExecuteForRecover_CallCashOutApiSuccess_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketRecoverEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForRecover(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(1).UndoVoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task ExecuteForRecover_HasException_CalledExpected()
    {
        // Arrange
        var emailTickets = new List<TicketRecoverEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            BroadcastSessionId = "1"
        };
        const string error = "ERROR: CallCashOutApiStage.ExecuteForRecover - SessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        InItService();
        Service = new CallCashOutApiStageImp(null, null, loggingService);

        // Act
        await Service.ExecuteForRecover(automationInfo, emailTickets);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
    }

    [Fact]
    public async Task ExecuteForRecover_IsRunExternalService_NotCallCashOutApi()
    {
        // Arrange
        var emailTickets = new List<TicketRecoverEmailModel>
        {
            new() { IsCashOut = true, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = true,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForRecover(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task ExecuteForRecover_NoHasTicketCashOut_NotCallCashOutApi()
    {
        // Arrange
        var emailTickets = new List<TicketRecoverEmailModel>
        {
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball, TransId = 1, CustId = 1},
            new() { IsCashOut = true, SportType = 1},
            new() { IsCashOut = false, SportType = SportTypeConstant.Basketball}
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1
        };
        InItService();
        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, null);

        // Act
        await Service.ExecuteForRecover(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }
}