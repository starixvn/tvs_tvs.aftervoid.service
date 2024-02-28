using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class UpdateEmailResultStageTests
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    public UpdateEmailResultStage Service;

    public UpdateEmailResultStageTests()
    {
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
    }

    [Fact]
    public async Task Execute_NotHasError_NotCalledUpdateEmailResultByVoidingHistory()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var currentEmailResults = new List<Result> { new() };
        Service = new UpdateEmailResultStageImp(null);

        // Act
        await Service.Execute(automationInfo, currentEmailResults);

        // Assert
        await voidBetAutomationService.Received(0).UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_HasError_CalledUpdateEmailResultByVoidingHistory()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestId = 1,
            VoidingHistoryId = 1
        };
        var currentEmailResults = new List<Result>
        {
            new(){ ErrorCode = ErrorCode.SendEmailToLicenseeSuccess },
            new(){ ErrorCode = ErrorCode.SendEmailToLicenseeFail },
            new(){ ErrorCode = ErrorCode.LicenseeSuccess },
            new(){ ErrorCode = ErrorCode.LicenseeFail }
        };
        Service = new UpdateEmailResultStageImp(voidBetAutomationService);

        // Act
        await Service.Execute(automationInfo, currentEmailResults);

        // Assert
        await voidBetAutomationService.Received(1).UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, Arg.Any<string>());
    }
}