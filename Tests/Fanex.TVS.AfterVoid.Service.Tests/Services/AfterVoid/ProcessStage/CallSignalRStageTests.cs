using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.ProcessStage;

public class CallSignalRStageTests
{
    private NotificationSignalRService notificationSignalRService;
    public CallSignalRStageImp Service;

    [Fact]
    public async Task ExecuteForAutoVoid_HasValidData_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestId = 1,
            MatchId = 1
        };
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForAutoVoid(true, RequestStatusConstant.Completed, automationInfo);

        // Assert
        await notificationSignalRService.Received(1).UpdateCompletedStatusForRequests(1, 1);
    }

    [Theory]
    [InlineData(false, "Completed")]
    [InlineData(true, "Failed")]
    [InlineData(false, "Failed")]
    public async Task ExecuteForAutoVoid_NotValidData_NotCalledUpdateCompletedStatusForRequests(bool isLastedInRequest, string currentRequestStatus)
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestId = 1,
            MatchId = 1
        };
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForAutoVoid(isLastedInRequest, currentRequestStatus, automationInfo);

        // Assert
        await notificationSignalRService.Received(0).UpdateCompletedStatusForRequests(1, 1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public async Task ExecuteForManualVoid_HasValidData_CalledExpected(int matchId)
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestId = 1,
            MatchId = matchId
        };
        var currentEmailResults = new List<Result>
        {
            new() { ErrorCode = ErrorCode.LicenseeSuccess, Data = 1 },
            new() { ErrorCode = ErrorCode.LicenseeFail, Data = 2 }
        };
        var emailResultsHistory = new List<EmailResultModel>
        {
            new() { Type = ErrorCode.LicenseeSuccess, Licensees = "a" },
            new() { Type = ErrorCode.LicenseeFail, Licensees = "b" }
        };
        var parameter = new CallSignalRManualVoidParameter
        {
            IsLastedInRequest = true,
            CurrentRequestStatus = RequestStatusConstant.Completed,
            AutomationInfo = automationInfo,
            CreatorName = "a",
            EmailResultsHistory = emailResultsHistory,
            CurrentEmailResults = currentEmailResults
        };
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForManualVoid(parameter);

        // Assert
        await notificationSignalRService.Received(1).UpdateCompletedStatusForRequests(1, matchId);
        await notificationSignalRService.Received(1).NotifyVoidResultToUser("a", Arg.Any<List<Result>>());
    }

    [Theory]
    [InlineData(true, RequestStatusConstant.Completed, true)]
    [InlineData(true, RequestStatusConstant.Completed, false)]
    [InlineData(true, RequestStatusConstant.Running, true)]
    [InlineData(true, RequestStatusConstant.Running, false)]
    public async Task ExecuteForAutoRecover_HasValidData_CalledSignalR(bool isLastedInRequest, string currentRequestStatus, bool isHasResettledTicket)
    {
        // Arrange
        var requestInfo = new RequestDetailViewModel
        {
            Request = new RequestModel
            {
                IsHasResettledTicket = isHasResettledTicket,
                RequestedBy = "a"
            }
        };
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestInfo = requestInfo,
            MatchId = 1,
            RequestId = 1
        };
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForAutoRecover(isLastedInRequest, currentRequestStatus, automationInfo);

        // Assert
        await notificationSignalRService.Received(1).UpdateCompletedStatusForRequests(1, 1);
        await notificationSignalRService.Received(1).NotifyVoidResultToUser("a", Arg.Any<List<Result>>());
    }

    [Theory]
    [InlineData(false, RequestStatusConstant.Completed, true)]
    [InlineData(false, RequestStatusConstant.Completed, false)]
    [InlineData(false, RequestStatusConstant.Running, true)]
    [InlineData(false, RequestStatusConstant.Running, false)]
    [InlineData(false, RequestStatusConstant.Failed, false)]
    public async Task ExecuteForAutoRecover_HasValidData_NotCalledSignalR(bool isLastedInRequest, string currentRequestStatus, bool isHasResettledTicket)
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForAutoRecover(isLastedInRequest, currentRequestStatus, automationInfo);

        // Assert
        await notificationSignalRService.Received(0).UpdateCompletedStatusForRequests(1, 1);
        await notificationSignalRService.Received(0).NotifyVoidResultToUser("a", Arg.Any<List<Result>>());
    }

    [Theory]
    [InlineData(false, "Completed")]
    [InlineData(true, "Failed")]
    [InlineData(false, "Failed")]
    public async Task ExecuteForManualVoid_NotValidData_NotCalledUpdateCompletedStatusForRequests(bool isLastedInRequest, string currentRequestStatus)
    {
        // Arrange
        var parameter = new CallSignalRManualVoidParameter
        {
            IsLastedInRequest = isLastedInRequest,
            CurrentRequestStatus = currentRequestStatus
        };
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        Service = new CallSignalRStageImp(notificationSignalRService);

        // Act
        await Service.ExecuteForManualVoid(parameter);

        // Assert
        await notificationSignalRService.Received(0).UpdateCompletedStatusForRequests(1, 1);
    }
}