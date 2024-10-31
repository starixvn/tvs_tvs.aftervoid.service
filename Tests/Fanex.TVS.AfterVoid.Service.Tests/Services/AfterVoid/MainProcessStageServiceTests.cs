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

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid;

public class MainProcessStageServiceTests
{
    private CreateMessageStage createMessageStage;
    private CreateVoidReasonStage createVoidReasonStage;
    private SendBatchMessageStage sendBatchMessageStage;
    private SendMessageToAdminSiteStage sendMessageToAdminSiteStage;
    private InsertVoidedTransactionStage insertVoidedTransactionStage;
    private UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;
    private SendEmailStage sendEmailStage;
    private VoidBetAutomationService voidBetAutomationService;
    public MainProcessStageService Service;

    internal void InItService()
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
        InItService();
        createMessageStage.Execute(Arg.Any<CreateMessageStageParameter>()).ReturnsForAnyArgs((1, 2, new Message(), new Message()));
        Service = new MainProcessStageServiceImp(createMessageStage, null, null, null, null, null, null, null);

        // Act
        _ = await Service.CreateMessage(null);

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
        InItService();
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
        InItService();
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
        InItService();
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
        InItService();
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, null);

        // Act
        var actual = Service.IsHaveVoidReason(voidedReason);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IsHaveVoidReason_HasVoidedReasonIsNull_ReturnFalse()
    {
        // Arrange
        InItService();
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, null);

        // Act
        var actual = Service.IsHaveVoidReason(null);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public async Task SendBatchMessage_CalledExpected()
    {
        // Arrange
        InItService();
        sendBatchMessageStage.Execute(Arg.Any<SendBatchMessageStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        _ = await Service.SendBatchMessage(true, default);

        // Assert
        await sendBatchMessageStage.Execute(Arg.Any<SendBatchMessageStageParameter>());
    }

    [Fact]
    public async Task SendBatchMessage_NotCalledExpected()
    {
        // Arrange
        InItService();
        sendBatchMessageStage.Execute(Arg.Any<SendBatchMessageStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, null);

        // Act
        _ = await Service.SendBatchMessage(false, default);

        // Assert
        await createVoidReasonStage.Received(0).Execute(Arg.Any<CreateVoidReasonStageParameter>());
    }

    [Fact]
    public async Task SendMessageToAdminSite_IssueTypeNotAbnormalBet_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            RequestInfo = new RequestDetailViewModel
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.DangerAccepted } }
        };
        InItService();
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, sendMessageToAdminSiteStage, null, null, null, null);

        // Act
        _ = await Service.SendMessageToAdminSite(true, automationInfo, new Message());

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
            { Request = new RequestModel { IssueTypeId = IssueTypeConstant.AbnormalBet } }
        };
        InItService();
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, sendMessageToAdminSiteStage, null, null, null, null);

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
        InItService();
        sendMessageToAdminSiteStage.Execute(automationInfo, Arg.Any<Message>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, sendBatchMessageStage, null, null, null, null, null);

        // Act
        var actual = await Service.SendMessageToAdminSite(false, automationInfo, default);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public async Task InsertVoidedTransaction_CalledExpected()
    {
        // Arrange
        InItService();
        insertVoidedTransactionStage.Execute(Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, null, insertVoidedTransactionStage, null, null, null);

        // Act
        _ = await Service.InsertVoidedTransaction(true, default);

        // Assert
        await insertVoidedTransactionStage.Received(1).Execute(Arg.Any<InsertVoidedTransactionStageParameter>());
    }

    [Fact]
    public async Task InsertVoidedTransaction_NotCalledExpected()
    {
        // Arrange
        InItService();
        insertVoidedTransactionStage.Execute(Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, null, insertVoidedTransactionStage, null, null, null);

        // Act
        _ = await Service.InsertVoidedTransaction(false, default);

        // Assert
        await insertVoidedTransactionStage.Received(0).Execute(Arg.Any<InsertVoidedTransactionStageParameter>());
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_CalledExpected()
    {
        // Arrange
        InItService();
        updateTicketVoidingHistoryStage.Execute(Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, null, null, updateTicketVoidingHistoryStage, null, null);

        // Act
        _ = await Service.UpdateTicketVoidingHistory(true, default);

        // Assert
        await updateTicketVoidingHistoryStage.Received(1).Execute(Arg.Any<UpdateTicketVoidingHistoryStageParameter>());
    }

    [Fact]
    public async Task UpdateTicketVoidingHistory_NotCalledExpected()
    {
        // Arrange
        InItService();
        updateTicketVoidingHistoryStage.Execute(Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(true);
        Service = new MainProcessStageServiceImp(null, null, null, null, null, updateTicketVoidingHistoryStage, null, null);

        // Act
        _ = await Service.UpdateTicketVoidingHistory(false, default);

        // Assert
        await updateTicketVoidingHistoryStage.Received(0).Execute(Arg.Any<UpdateTicketVoidingHistoryStageParameter>());
    }

    [Fact]
    public async Task SendEmail_CalledExpected()
    {
        // Arrange
        InItService();
        sendEmailStage.Execute(Arg.Any<SendEmailStageParameter>()).ReturnsForAnyArgs((true, null));
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, sendEmailStage, null);

        // Act
        _ = await Service.SendEmail(true, default);

        // Assert
        await sendEmailStage.Received(1).Execute(Arg.Any<SendEmailStageParameter>());
    }

    [Fact]
    public async Task SendEmail_NotCalledExpected()
    {
        // Arrange
        InItService();
        sendEmailStage.Execute(Arg.Any<SendEmailStageParameter>()).ReturnsForAnyArgs((true, null));
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, sendEmailStage, null);

        // Act
        _ = await Service.SendEmail(false, default);

        // Assert
        await sendEmailStage.Received(0).Execute(Arg.Any<SendEmailStageParameter>());
    }

    [Fact]
    public async Task GetMatchInfo_HasMatchId_CalledExpected()
    {
        // Arrange
        InItService();
        voidBetAutomationService.GetMatchInfo(1).ReturnsForAnyArgs(new Match());
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, voidBetAutomationService);

        // Act
        _ = await Service.GetMatchInfo(1);

        // Assert
        await voidBetAutomationService.Received(1).GetMatchInfo(1);
    }

    [Fact]
    public async Task GetMatchInfo_NotHasMatchId_NotCalledExpected()
    {
        // Arrange
        InItService();
        voidBetAutomationService.GetMatchInfo(Arg.Any<int>()).ReturnsForAnyArgs(new Match());
        Service = new MainProcessStageServiceImp(null, null, null, null, null, null, null, voidBetAutomationService);

        // Act
        _ = await Service.GetMatchInfo(0);

        // Assert
        await voidBetAutomationService.Received(0).GetMatchInfo(Arg.Any<int>());
    }
}