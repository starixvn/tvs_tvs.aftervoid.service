using System;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage.AutoRecover;

public class DeleteZzzBetTransStageTests : IClassFixture<AppSettingFixture>
{
    [Fact]
    public async Task Execute_HadDeletedMessage_ReturnTrue()
    {
        // Arrange
        var requestViewModel = new RequestDetailViewModel
        {
            EventByMatch = new RequestEventModel()
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = true,
            RequestId = 1,
            VoidingHistoryId = 1,
            RequestInfo = requestViewModel
        };
        var repository = Substitute.For<IDynamicRepository>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        repository.ExecuteAsync(null).ReturnsForAnyArgs(1);
        var service = new DeleteZzzBetTransStageImp(repository, voidBetAutomationService, null, null);

        // Act
        var actual = await service.Execute(automationInfo, "123");

        // Assert
        Assert.True(actual);
        await repository.Received().ExecuteAsync(Arg.Any<DeleteZzzBetTransCommand>());
        await voidBetAutomationService.Received(1).UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, Arg.Any<VoidBetAutomationInfo>());
    }

    [Fact]
    public async Task Execute_HadException_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            IsSentEmail = true,
            RequestId = 1,
            VoidingHistoryId = 1,
            BroadcastSessionId = "1"
        };
        var loggingService = Substitute.For<LoggingService>();
        var notificationSignalRService = Substitute.For<NotificationSignalRService>();
        var voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        const string error = "ERROR: DeleteZzzBetTransStage.Execute - BroadcastSessionId: 1 - VDHistoryId: 1 - RequestId: 1";

        var service = new DeleteZzzBetTransStageImp(null, voidBetAutomationService, notificationSignalRService, loggingService);

        // Act
        var actual = await service.Execute(automationInfo, "1");

        // Assert
        Assert.False(actual);
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
    }
}