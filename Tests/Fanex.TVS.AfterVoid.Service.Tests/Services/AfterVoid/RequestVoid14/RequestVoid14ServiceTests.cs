using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestVoid14;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.RequestVoid14;

public class RequestVoid14ServiceTests
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly MainProcessStageService mainProcessStageService;
    private readonly CallCashOutApiStage callCashOutApiStage;
    private readonly WriteLogAfterVoidStage writeLogAfterVoidStage;
    private readonly CallSignalRStage callSignalRStage;
    public RequestVoid14Service Service;

    public RequestVoid14ServiceTests()
    {
        mainProcessStageService = Substitute.For<MainProcessStageService>();
        voidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        callCashOutApiStage = Substitute.For<CallCashOutApiStage>();
        writeLogAfterVoidStage = Substitute.For<WriteLogAfterVoidStage>();
        callSignalRStage = Substitute.For<CallSignalRStage>();
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
        Service = new RequestVoid14ServiceImp(null, null, null, null, null, null);

        // Act
        await Service.Execute(automationInfo, true, "");

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
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() };
        var voidedTickets = new List<TicketEmailModel>
        {
            new(),
            new()
        };

        var voidReason = new VoidedReason();
        const bool isSentMessageToAdminSite = true;
        const bool isInsertedVoidedTransaction = true;
        const bool isUpdatedVoidingHistory = true;
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId).ReturnsForAnyArgs(allVoidDataInCurrentVoid);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true).ReturnsForAnyArgs(voidedTickets);
        mainProcessStageService.CreateVoidReason(true, Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs(voidReason);
        mainProcessStageService.IsHaveVoidReason(Arg.Any<VoidedReason>()).ReturnsForAnyArgs(true);
        mainProcessStageService.InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(isInsertedVoidedTransaction);
        mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(isUpdatedVoidingHistory);
        Service = new RequestVoid14ServiceImp(voidBetAutomationService, mainProcessStageService, notificationSignalRService, callCashOutApiStage, writeLogAfterVoidStage, callSignalRStage);

        // Act
        await Service.Execute(automationInfo, true, "");

        // Assert
        await voidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        await voidBetAutomationService.Received(1).GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        await mainProcessStageService.Received(1).CreateVoidReason(true, Arg.Any<CreateVoidReasonStageParameter>());
        await mainProcessStageService.Received(1).InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>());
        await mainProcessStageService.Received(1).UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>());
        await callCashOutApiStage.Received(1).Execute(automationInfo, Arg.Any<IEnumerable<TicketEmailModel>>());
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
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() { CreatorName = "a" } };
        var voidedTickets = new List<TicketEmailModel>
        {
            new(),
            new()
        };

        const bool isSentMessageToAdminSite = true;
        const bool isInsertedVoidedTransaction = true;
        const bool isUpdatedVoidingHistory = false;
        voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId).ReturnsForAnyArgs(allVoidDataInCurrentVoid);
        voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true).ReturnsForAnyArgs(voidedTickets);
        mainProcessStageService.CreateVoidReason(true, Arg.Any<CreateVoidReasonStageParameter>()).ReturnsForAnyArgs((VoidedReason)null);
        mainProcessStageService.IsHaveVoidReason(Arg.Any<VoidedReason>()).ReturnsForAnyArgs(true);
        mainProcessStageService.InsertVoidedTransaction(isSentMessageToAdminSite, Arg.Any<InsertVoidedTransactionStageParameter>()).ReturnsForAnyArgs(isInsertedVoidedTransaction);
        mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(isUpdatedVoidingHistory);
        Service = new RequestVoid14ServiceImp(voidBetAutomationService, mainProcessStageService, notificationSignalRService, callCashOutApiStage, writeLogAfterVoidStage, callSignalRStage);

        // Act
        await Service.Execute(automationInfo, true, "");

        // Assert
        await notificationSignalRService.Received(1).NotifyErrorWhenExecuteAfterVoidServiceToUser("a");
        await callCashOutApiStage.Received(0).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketEmailModel>>());
        await voidBetAutomationService.Received(0).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.Received(0).ExecuteForManualVoid(Arg.Any<CallSignalRManualVoidParameter>());
        await writeLogAfterVoidStage.Received(0).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<Result>>());
    }
}