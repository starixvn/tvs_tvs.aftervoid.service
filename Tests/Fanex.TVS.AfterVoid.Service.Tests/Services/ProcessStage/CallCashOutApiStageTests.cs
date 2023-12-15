using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.ProcessStage;

public class CallCashOutApiStageTests
{
    private readonly LoggingService loggingService;
    private readonly CashOutHttpClientService cashOutHttpClientService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    public CallCashOutApiStage Service;

    public CallCashOutApiStageTests()
    {
        loggingService = Substitute.For<LoggingService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        cashOutHttpClientService = Substitute.For<CashOutHttpClientService>();
    }

    [Fact]
    public async Task Execute_CallCashOutApiSuccess_CalledExpected()
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

        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, loggingService);

        // Act
        await Service.Execute(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(1).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
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
        const string error = "ERROR: CallCashOutApiStage.Execute - SessionId: 1 - VDHistoryId: 1 - RequestId: 1";
        Service = new CallCashOutApiStageImp(voidBetAutomationService, null, loggingService);

        // Act
        await Service.Execute(automationInfo, emailTickets);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
    }

    [Fact]
    public async Task Execute_IsRunExternalService_NotCallCashOutApi()
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

        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, loggingService);

        // Act
        await Service.Execute(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task Execute_NoHasTicketCashOut_NotCallCashOutApi()
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

        Service = new CallCashOutApiStageImp(voidBetAutomationService, cashOutHttpClientService, loggingService);

        // Act
        await Service.Execute(automationInfo, emailTickets);

        // Assert
        await cashOutHttpClientService.Received(0).VoidBet(Arg.Any<CashOutApiModel>());
        await voidBetAutomationService.Received(0).UpdateAfterVoidStep(1, 1, Arg.Any<VoidBetAutomationInfo>());
    }
}