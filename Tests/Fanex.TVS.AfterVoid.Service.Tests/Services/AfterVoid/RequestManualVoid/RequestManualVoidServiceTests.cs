using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestManualVoid;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.RequestManualVoid;

public class RequestManualVoidServiceTests
{
    private VoidBetAutomationService voidBetAutomationService;
    private MainProcessStageService mainProcessStageService;
    private NotificationSignalRService notificationSignalRService;
    private SpecialPlatformService specialPlatformService;
    private UpdateEmailResultStage updateEmailResultStage;
    private CallCashOutApiStage callCashOutApiStage;
    private WriteLogAfterVoidStage writeLogAfterVoidStage;
    private CallSignalRStage callSignalRStage;
    public RequestManualVoidService Service;

    internal void InItService()
    {
        mainProcessStageService = Substitute.For<MainProcessStageService>();
        updateEmailResultStage = Substitute.For<UpdateEmailResultStage>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        callCashOutApiStage = Substitute.For<CallCashOutApiStage>();
        writeLogAfterVoidStage = Substitute.For<WriteLogAfterVoidStage>();
        callSignalRStage = Substitute.For<CallSignalRStage>();
        specialPlatformService = Substitute.For<SpecialPlatformService>();
        notificationSignalRService = Substitute.For<NotificationSignalRService>();
    }

    [Fact]
    public async Task Execute_HasStatusDone_NotCreateMessage()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.Done
        };
        InItService();
        Service = new RequestManualVoidServiceImp(null, null, null, null, null, null, null, null);

        // Act
        await Service.Execute(automationInfo, true, "", null);

        // Assert
        await mainProcessStageService.Received(0).CreateMessage(Arg.Any<CreateMessageStageParameter>());
    }

    [Fact]
    public async Task Execute_HasStatusNotDone_CalledExpected()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.New,
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        };
        var matchInfo = new Match { MatchId = 1, SportType = 1 };
        var voidDataXml = new VoidDataXmlModel();
        var voidedTickets = new List<TicketEmailModel>
        {
            new(),
            new()
        };
        var normalTickets = new List<TicketEmailModel>
        {
            new(){TransId = 1}
        };
        var specialTickets = new List<TicketEmailModel>
        {
            new(){TransId = 2}
        };
        var normalMessageContent = new Message();
        var specialMessageContent = new Message();
        const int normalMessageId = 1;
        const int specialMessageId = 2;
        var voidReason = new VoidedReason();
        const bool isSendBatchMessage = true;
        const bool isSentMessageToAdminSite = true;
        const bool isInsertedVoidedTransaction = true;
        const bool isUpdatedVoidingHistory = true;
        const bool isSentEmail = true;
        InItService();
        mainProcessStageService.GetMatchInfo(automationInfo.MatchId).ReturnsForAnyArgs(matchInfo);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId).ReturnsForAnyArgs(voidDataXml);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true).ReturnsForAnyArgs(voidedTickets);
        specialPlatformService.GroupTicketSaba(Arg.Any<IEnumerable<TicketEmailModel>>()).ReturnsForAnyArgs((normalTickets, specialTickets));
        mainProcessStageService.CreateMessage(Arg.Any<CreateMessageStageParameter>()).ReturnsForAnyArgs((normalMessageId, specialMessageId, normalMessageContent, specialMessageContent));
        mainProcessStageService.IsHaveMessage(normalMessageId, specialMessageId).ReturnsForAnyArgs(true);
        mainProcessStageService.CreateVoidReason(true, Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs(voidReason);
        mainProcessStageService.IsHaveVoidReason(Arg.Any<VoidedReason>()).ReturnsForAnyArgs(true);
        mainProcessStageService.SendBatchMessage(true, Arg.Any<SendBatchMessageStageParameter>())
            .ReturnsForAnyArgs(isSendBatchMessage);
        mainProcessStageService.SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent)
            .ReturnsForAnyArgs(isSentMessageToAdminSite);
        mainProcessStageService.InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(isInsertedVoidedTransaction);
        mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(isUpdatedVoidingHistory);
        mainProcessStageService.SendEmail(isUpdatedVoidingHistory, Arg.Any<SendEmailStageParameter>()).ReturnsForAnyArgs((isSentEmail, null));
        Service = new RequestManualVoidServiceImp(voidBetAutomationService, mainProcessStageService, notificationSignalRService, specialPlatformService,
            updateEmailResultStage, callCashOutApiStage, writeLogAfterVoidStage, callSignalRStage);

        // Act
        await Service.Execute(automationInfo, true, "", null);

        // Assert
        await mainProcessStageService.Received(1).GetMatchInfo(automationInfo.MatchId);
        await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        specialPlatformService.Received(1).GroupTicketSaba(Arg.Any<IEnumerable<TicketEmailModel>>());
        await mainProcessStageService.Received(1).CreateMessage(Arg.Any<CreateMessageStageParameter>());
        await mainProcessStageService.Received(1).SendBatchMessage(true, Arg.Any<SendBatchMessageStageParameter>());
        await mainProcessStageService.Received(1).SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent);
        await mainProcessStageService.Received(1).InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>());
        await mainProcessStageService.Received(1).UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>());
        await mainProcessStageService.Received(1).SendEmail(isUpdatedVoidingHistory, Arg.Any<SendEmailStageParameter>());
        await updateEmailResultStage.Received(1).Execute(automationInfo, Arg.Any<IEnumerable<Result>>());
        await callCashOutApiStage.Received(1).ExecuteForVoid(automationInfo, Arg.Any<IEnumerable<TicketEmailModel>>());
        await voidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.Received(1).ExecuteForManualVoid(Arg.Any<CallSignalRManualVoidParameter>());
        await writeLogAfterVoidStage.Received(1).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<Result>>());
    }

    [Fact]
    public async Task Execute_NotSentEmail_NotUpdateEmailResult()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.New,
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        };
        var voidDataXml = new VoidDataXmlModel { CreatorName = "a" };
        var voidedTickets = new List<TicketEmailModel>
        {
            new(),
            new()
        };
        var normalTickets = new List<TicketEmailModel>
        {
            new(){TransId = 1}
        };
        var specialTickets = new List<TicketEmailModel>
        {
            new(){TransId = 2}
        };
        var specialMessageContent = new Message();
        var normalMessageContent = new Message();
        const int normalMessageId = 1;
        const int specialMessageId = 2;
        const bool isSendBatchMessage = true;
        const bool isSentMessageToAdminSite = true;
        const bool isInsertedVoidedTransaction = true;
        const bool isUpdatedVoidingHistory = true;
        const bool isSentEmail = false;
        InItService();
        mainProcessStageService.GetMatchInfo(automationInfo.MatchId).ReturnsForAnyArgs((Match)null);
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId).ReturnsForAnyArgs(voidDataXml);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true).ReturnsForAnyArgs(voidedTickets);
        specialPlatformService.GroupTicketSaba(Arg.Any<IEnumerable<TicketEmailModel>>()).ReturnsForAnyArgs((normalTickets, specialTickets));
        mainProcessStageService.CreateMessage(Arg.Any<CreateMessageStageParameter>()).ReturnsForAnyArgs((normalMessageId, specialMessageId, normalMessageContent, specialMessageContent));
        mainProcessStageService.IsHaveMessage(normalMessageId, specialMessageId).ReturnsForAnyArgs(true);
        mainProcessStageService.CreateVoidReason(true, Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs((VoidedReason)null);
        mainProcessStageService.IsHaveVoidReason(Arg.Any<VoidedReason>()).ReturnsForAnyArgs(true);
        mainProcessStageService.SendBatchMessage(true, Arg.Any<SendBatchMessageStageParameter>())
            .ReturnsForAnyArgs(isSendBatchMessage);
        mainProcessStageService.SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent)
            .ReturnsForAnyArgs(isSentMessageToAdminSite);
        mainProcessStageService.InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(isInsertedVoidedTransaction);
        mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(isUpdatedVoidingHistory);
        mainProcessStageService.SendEmail(isUpdatedVoidingHistory, Arg.Any<SendEmailStageParameter>()).ReturnsForAnyArgs((isSentEmail, null));
        Service = new RequestManualVoidServiceImp(voidBetAutomationService, mainProcessStageService, notificationSignalRService, specialPlatformService,
            updateEmailResultStage, callCashOutApiStage, writeLogAfterVoidStage, callSignalRStage);

        // Act
        await Service.Execute(automationInfo, true, "", null);

        // Assert
        await updateEmailResultStage.Received(0).Execute(automationInfo, Arg.Any<IEnumerable<Result>>());
        await callCashOutApiStage.Received(0).ExecuteForVoid(automationInfo, Arg.Any<IEnumerable<TicketEmailModel>>());
        await voidBetAutomationService.Received(0).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.Received(0).ExecuteForManualVoid(Arg.Any<CallSignalRManualVoidParameter>());
        await writeLogAfterVoidStage.Received(0).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<Result>>());
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser(Arg.Any<string>());
    }
}