using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class WriteLogAfterVoidStageTests
{
    private LoggingService loggingService;
    public WriteLogAfterVoidStage Service;

    [Fact]
    public async Task Execute_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = false,
            VoidingHistoryId = 1,
            RequestId = 1,
            BroadcastSessionId = "1"
        };
        var results = new List<Result> { new() { ErrorCode = 1, ErrorMsg = "111" } };
        loggingService = Substitute.For<LoggingService>();
        Service = new WriteLogAfterVoidStageImp(loggingService);

        // Act
        await Service.Execute(automationInfo, results);

        // Assert
        await loggingService.Received(1).InfoAsync(Arg.Any<string>());
    }
}