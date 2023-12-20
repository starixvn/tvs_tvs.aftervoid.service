using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid
{
    internal class MainProcessStageServiceTests
    {
    }
}

public class MainProcessStageServiceTests
{
    private readonly CreateMessageStage createMessageStage;
    private readonly CreateVoidReasonStage createVoidReasonStage;
    private readonly SendBatchMessageStage sendBatchMessageStage;
    private readonly SendMessageToAdminSiteStage sendMessageToAdminSiteStage;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;
    private readonly SendEmailStage sendEmailStage;
    private readonly VoidBetAutomationService voidBetAutomationService;
    public MainProcessStageService Service;

    public MainProcessStageServiceTests()
    {
        createMessageStage = Substitute.For<CreateMessageStage>();
        createVoidReasonStage = Substitute.For<CreateVoidReasonStage>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        sendBatchMessageStage = Substitute.For<SendBatchMessageStage>();
        sendMessageToAdminSiteStage = Substitute.For<SendMessageToAdminSiteStage>();
        insertVoidedTransactionStage = Substitute.For<InsertVoidedTransactionStage>();
        updateTicketVoidingHistoryStage = Substitute.For<UpdateTicketVoidingHistoryStage>();
        sendEmailStage = Substitute.For<SendEmailStage>();
    }

    [Fact]
    public async Task CreateMessage_CalledExpected()
    {
        // Arrange
        createMessageStage.Execute(Arg.Any<CreateMessageStageParameter>()).ReturnsForAnyArgs((1, 2, new Message()));
        Service = new MainProcessStageServiceImp(createMessageStage, null, null, null, null, null, null, null);

        // Act
        (_, _, _) = await Service.CreateMessage(null);

        // Assert
        await createMessageStage.Received(1).Execute(Arg.Any<CreateMessageStageParameter>());
    }

    [Theory]
    [InlineData(1, 0, true)]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, true)]
    [InlineData(1, 1, true)]
    public void IsHaveMessage_ReturnExpected(int normalMessageId, int specialMessageId, bool expected)
    {
        // Arrange
        Service = new MainProcessStageServiceImp(createMessageStage, null, null, null, null, null, null, null);

        // Act
        var actual = Service.IsHaveMessage(normalMessageId, specialMessageId);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CreateVoidReason_CalledExpected()
    {
        // Arrange
        createVoidReasonStage.Execute(Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs(new VoidedReason());
        Service = new MainProcessStageServiceImp(null, createVoidReasonStage, null, null, null, null, null, null);

        // Act
        _ = await Service.CreateVoidReason(true, default);

        // Assert
        await createVoidReasonStage.Received(1).Execute(Arg.Any<CreateVoidReasonStageParameter>());
    }

    [Fact]
    public async Task CreateVoidReason_NotCalledExpected()
    {
        // Arrange
        createVoidReasonStage.Execute(Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs(new VoidedReason());
        Service = new MainProcessStageServiceImp(null, createVoidReasonStage, null, null, null, null, null, null);

        // Act
        _ = await Service.CreateVoidReason(false, default);

        // Assert
        await createVoidReasonStage.Received(0).Execute(Arg.Any<CreateVoidReasonStageParameter>());
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsHaveVoidReason_ReturnExpected(int reasonId, bool expected)
    {
        // Arrange
        var voidedReason = new VoidedReason { ReasonId = reasonId };
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, null);

        // Act
        var actual = Service.IsHaveVoidReason(voidedReason);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SendBatchMessage_CalledExpected()
    {
        // Arrange
        sendBatchMessageStage.Execute(Arg.Any<SendBatchMessageStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        _ = await Service.SendBatchMessage(true, default);

        // Assert
        await createVoidReasonStage.Received(1).Execute(Arg.Any<CreateVoidReasonStageParameter>());
    }

    [Fact]
    public async Task SendBatchMessage_NotCalledExpected()
    {
        // Arrange
        sendBatchMessageStage.Execute(Arg.Any<SendBatchMessageStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        _ = await Service.SendBatchMessage(false, default);

        // Assert
        await createVoidReasonStage.Received(0).Execute(Arg.Any<CreateVoidReasonStageParameter>());
    }

    [Fact]
    public async Task SendMessageToAdminSite_HasIssueTypeIsAbnormalBet_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet } }
        };
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        _ = await Service.SendMessageToAdminSite(true, automationInfo, default);

        // Assert
        await sendMessageToAdminSiteStage.Received(1).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<Message>());
    }

    [Fact]
    public async Task SendMessageToAdminSite_HasIssueTypeNotAbnormalBet_NotCalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } }
        };
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        var actual = await Service.SendMessageToAdminSite(true, automationInfo, default);

        // Assert
        Assert.True(actual);
        await sendMessageToAdminSiteStage.Received(0).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<Message>());
    }

    [Fact]
    public async Task SendMessageToAdminSite_returnFalse()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet } }
        };
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        var actual = await Service.SendMessageToAdminSite(false, automationInfo, default);

        // Assert
        Assert.False(actual);
    }
}