using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.ProcessStage;

public class CreateMessageStageTests
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendLetstalkServiceStage sendLetstalkServiceStage;
    private readonly SendMessageService sendMessageService;
    private readonly SpecialPlatformService specialPlatformService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    public CreateMessageStage Service;

    public CreateMessageStageTests()
    {
        loggingService = Substitute.For<LoggingService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
        sendLetstalkServiceStage = Substitute.For<SendLetstalkServiceStage>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendMessageService = Substitute.For<SendMessageService>();
        specialPlatformService = Substitute.For<SpecialPlatformService>();
    }

    [Fact]
    public async Task Execute_HasTicketsNormalSiteAndSpecialSite_CalledExpected()
    {
        // Arrange
        var matchInfo = new Match { MatchId = 1, LeagueGroupId = 1 };
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var specialTickets = new List<TicketEmailModel> { new() };
        var normalTickets = new List<TicketEmailModel> { new() };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            IsAutoVoid = true,
            RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 1 } },
            MessageIdSpecial = null,
            MessageId = null,
        };
        var parameter = new CreateMessageStageParameter
        {
            AutomationInfo = automationInfo,
            AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
            NormalTickets = normalTickets,
            SpecialTickets = specialTickets,
            MatchInfo = matchInfo,
        };
        specialPlatformService.RemoveSabaInMessageContent(Arg.Any<Match>(), Arg.Any<Message>())
            .ReturnsForAnyArgs(new Message());
        sendMessageService.InsertMessage(Arg.Any<Message>(), 1, 1, 1).ReturnsForAnyArgs(1);
        Service = new CreateMessageStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, loggingService,
            sendLetstalkServiceStage, specialPlatformService);

        // Act
        await Service.Execute(parameter);

        // Assert
        await sendMessageService.Received(1).InitializeMessageContentForAutoVoid(Arg.Any<Match>(), Arg.Any<IEnumerable<VoidDataXmlModel>>(), Arg.Any<RequestDetailViewModel>());
        await sendMessageService.Received(2).InsertMessage(Arg.Any<Message>(), 1, 1, 1);
    }

    [Fact]
    public async Task Execute_HasOnlyTicketsNormalSite_CalledExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var specialTickets = new List<TicketEmailModel>();
        var normalTickets = new List<TicketEmailModel> { new() };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            IsAutoVoid = false,
            RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 1 } },
            MessageIdSpecial = 0,
            MessageId = 0,
        };
        var parameter = new CreateMessageStageParameter
        {
            AutomationInfo = automationInfo,
            AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
            NormalTickets = normalTickets,
            SpecialTickets = specialTickets,
            MatchInfo = null,
        };
        sendMessageService.InitializeMessageContentForManualVoid(allVoidDataInCurrentVoid.First())
            .ReturnsForAnyArgs(new Message());
        sendMessageService.InsertMessage(Arg.Any<Message>(), 1, 1, 1).ReturnsForAnyArgs(1);
        Service = new CreateMessageStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, loggingService,
            sendLetstalkServiceStage, specialPlatformService);

        // Act
        var (normalMessageId, specialMessageId, _) = await Service.Execute(parameter);

        // Assert
        sendMessageService.Received(1).InitializeMessageContentForManualVoid(Arg.Any<VoidDataXmlModel>());
        await sendMessageService.Received(1).InsertMessage(Arg.Any<Message>(), 0, null, 1);
        Assert.Equal(1, normalMessageId);
        Assert.Equal(0, specialMessageId);
    }

    [Fact]
    public async Task Execute_HasOnlyTicketsSpecialSite_CalledExpected()
    {
        // Arrange
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var specialTickets = new List<TicketEmailModel> { new() };
        var normalTickets = new List<TicketEmailModel>();
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            IsAutoVoid = false,
            RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 1 } },
            MessageIdSpecial = null,
            MessageId = null,
        };
        var parameter = new CreateMessageStageParameter
        {
            AutomationInfo = automationInfo,
            AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
            NormalTickets = normalTickets,
            SpecialTickets = specialTickets,
            MatchInfo = null,
        };
        specialPlatformService.RemoveSabaInMessageContent(null, Arg.Any<Message>()).ReturnsForAnyArgs(new Message());
        sendMessageService.InitializeMessageContentForManualVoid(allVoidDataInCurrentVoid.First())
            .ReturnsForAnyArgs(new Message());
        sendMessageService.InsertMessage(Arg.Any<Message>(), 0, null, 1).ReturnsForAnyArgs(1);
        Service = new CreateMessageStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, loggingService,
            sendLetstalkServiceStage, specialPlatformService);

        // Act
        var (normalMessageId, specialMessageId, _) = await Service.Execute(parameter);

        // Assert
        sendMessageService.Received(1).InitializeMessageContentForManualVoid(Arg.Any<VoidDataXmlModel>());
        await sendMessageService.Received(1).InsertMessage(Arg.Any<Message>(), 0, null, 1);
        Assert.Equal(0, normalMessageId);
        Assert.Equal(1, specialMessageId);
    }

    [Fact]
    public async Task Execute_HasMessageIdAndMessageIdSpecial_CalledExpected()
    {
        // Arrange
        var matchInfo = new Match { MatchId = 1, LeagueGroupId = 1 };
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var specialTickets = new List<TicketEmailModel> { new() };
        var normalTickets = new List<TicketEmailModel> { new() };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            IsAutoVoid = true,
            RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 1 } },
            MessageIdSpecial = 2,
            MessageId = 1,
        };
        var parameter = new CreateMessageStageParameter
        {
            AutomationInfo = automationInfo,
            AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
            NormalTickets = normalTickets,
            SpecialTickets = specialTickets,
            MatchInfo = matchInfo,
        };
        sendMessageService.InitializeMessageContentForAutoVoid(Arg.Any<Match>(), Arg.Any<IEnumerable<VoidDataXmlModel>>(), Arg.Any<RequestDetailViewModel>())
            .ReturnsForAnyArgs(new Message());
        Service = new CreateMessageStageImp(voidBetAutomationService, sendMessageService, notificationSignalRService, loggingService,
            sendLetstalkServiceStage, specialPlatformService);

        // Act
        var (normalMessageId, specialMessageId, _) = await Service.Execute(parameter);

        // Assert
        await sendMessageService.Received(1).InitializeMessageContentForAutoVoid(Arg.Any<Match>(),
            Arg.Any<IEnumerable<VoidDataXmlModel>>(), Arg.Any<RequestDetailViewModel>());
        await sendMessageService.Received(0).InsertMessage(Arg.Any<Message>(), 1, 1, 1);
        Assert.Equal(1, normalMessageId);
        Assert.Equal(2, specialMessageId);
    }

    [Fact]
    public async Task Execute_HasException_CalledExpected()
    {
        // Arrange
        var matchInfo = new Match { MatchId = 1, LeagueGroupId = 1 };
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var specialTickets = new List<TicketEmailModel> { new() };
        var normalTickets = new List<TicketEmailModel> { new() };
        var automationInfo = new VoidBetAutomationInfo
        {
            IsRunExternalService = false,
            RequestId = 1,
            VoidingHistoryId = 1,
            IsAutoVoid = true,
            RequestInfo = new RequestDetailViewModel { Request = new RequestModel { IssueTypeId = 1 } },
            MessageIdSpecial = 2,
            MessageId = 1,
            BroadcastSessionId = "1"
        };
        var parameter = new CreateMessageStageParameter
        {
            AutomationInfo = automationInfo,
            AllVoidDataInCurrentVoid = allVoidDataInCurrentVoid,
            NormalTickets = normalTickets,
            SpecialTickets = specialTickets,
            MatchInfo = matchInfo,
        };
        sendMessageService.InitializeMessageContentForAutoVoid(Arg.Any<Match>(), Arg.Any<IEnumerable<VoidDataXmlModel>>(), Arg.Any<RequestDetailViewModel>())
            .ReturnsForAnyArgs(new Message());
        Service = new CreateMessageStageImp(voidBetAutomationService, null, notificationSignalRService, loggingService,
            sendLetstalkServiceStage, null);
        const string error = "ERROR: CreateMessageStage.Execute - BroadcastSessionId: 1";

        // Act
        var (normalMessageId, specialMessageId, message) = await Service.Execute(parameter);

        // Assert
        await loggingService.Received(1).ErrorAsync(error, Arg.Any<Exception>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId,
            automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await sendLetstalkServiceStage.Received(1).InsertLetstalk(automationInfo.RequestId, automationInfo.BroadcastSessionId, ExecuteStatus.Failed);
        await notificationSignalRService.Received(1).UpdateFailedStatusForRequest(automationInfo.RequestId);
        Assert.Equal(0, normalMessageId);
        Assert.Equal(0, specialMessageId);
        Assert.Null(message);
    }
}