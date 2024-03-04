using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoRecover;
using NSubstitute;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using Xunit;

namespace Fanex.TVS.AfterVoid.Service.Tests.Services.AfterVoid.RequestAutoRecover;

public class RequestAutoRecoverServiceTests
{
    public VoidBetAutomationService VoidBetAutomationService;
    public UpdateEmailResultStage UpdateEmailResultStage;
    public WriteLogAfterVoidStage WriteLogAfterVoidStage;
    public CallSignalRStage CallSignalRStage;
    public MainRecoverProcessStageService MainRecoverProcessStageService;
    public CallCashOutApiStage CallCashOutApiStage;
    public RequestAutoRecoverService Service;

    private void FakeInitDataForService(VoidBetAutomationService voidBetAutomationService,
        UpdateEmailResultStage updateEmailResultStage, WriteLogAfterVoidStage writeLogAfterVoidStage,
        CallSignalRStage callSignalRStage,
        MainRecoverProcessStageService mainRecoverProcessStageService, CallCashOutApiStage callCashOutApiStage)
    {
        Service = new RequestAutoRecoverServiceImp(voidBetAutomationService, updateEmailResultStage,
            writeLogAfterVoidStage, callSignalRStage,
            mainRecoverProcessStageService, callCashOutApiStage);
    }

    [Fact]
    public async Task Execute_HasStatusDone_NotDeletedMessage()
    {
        // Arrange
        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.Done
        };

        MainRecoverProcessStageService = Substitute.For<MainRecoverProcessStageService>();
        FakeInitDataForService(null, null, null, null, MainRecoverProcessStageService, null);

        // Act
        await Service.Execute(automationInfo, true, "");

        // Assert
        await MainRecoverProcessStageService.Received(0).DeleteMessage(Arg.Any<bool>(), Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_HasStatusNotDone_CalledExpected()
    {
        // Arrange
        UpdateEmailResultStage = Substitute.For<UpdateEmailResultStage>();
        VoidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        CallCashOutApiStage = Substitute.For<CallCashOutApiStage>();
        WriteLogAfterVoidStage = Substitute.For<WriteLogAfterVoidStage>();
        CallSignalRStage = Substitute.For<CallSignalRStage>();
        MainRecoverProcessStageService = Substitute.For<MainRecoverProcessStageService>();
        FakeInitDataForService(VoidBetAutomationService, UpdateEmailResultStage, WriteLogAfterVoidStage, CallSignalRStage, MainRecoverProcessStageService, CallCashOutApiStage);

        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.New,
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        };
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() { SingleTransIds = "1, 2" } };
        var recoveredTickets = new List<TicketRecoverEmailModel>
        {
            new(),
            new()
        };
        var currentEmailResults = new List<Result>();

        VoidBetAutomationService.GetVoidDataFromVoidingHistory(1).ReturnsForAnyArgs(allVoidDataInCurrentVoid);
        VoidBetAutomationService.GetRecoveredTicketsByVoidingHistory("1, 2", automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId).ReturnsForAnyArgs(recoveredTickets);
        MainRecoverProcessStageService.DeleteZzzBetTrans(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.DeleteMessage(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.DeleteTransaction(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<BetId>>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.SendEmail(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketRecoverEmailModel>>()).ReturnsForAnyArgs((true, currentEmailResults));
        MainRecoverProcessStageService.UpdateTicketVoidingHistory(true, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(true);
        FakeInitDataForService(VoidBetAutomationService, UpdateEmailResultStage, WriteLogAfterVoidStage, CallSignalRStage, MainRecoverProcessStageService, CallCashOutApiStage);

        // Act
        await Service.Execute(automationInfo, true, "");

        // Assert
        await VoidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(1);
        await VoidBetAutomationService.Received(1).GetRecoveredTicketsByVoidingHistory("1, 2", automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId);
        await MainRecoverProcessStageService.Received(1).DeleteZzzBetTrans(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>());
        await MainRecoverProcessStageService.Received(1).DeleteMessage(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>());
        await MainRecoverProcessStageService.Received(1).DeleteTransaction(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<BetId>>());
        await MainRecoverProcessStageService.Received(1).SendEmail(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketRecoverEmailModel>>());
        await MainRecoverProcessStageService.Received(1).UpdateTicketVoidingHistory(true, Arg.Any<UpdateTicketVoidingHistoryStageParameter>());
        await UpdateEmailResultStage.Received(1).Execute(automationInfo, Arg.Any<IEnumerable<Result>>());
        await CallCashOutApiStage.Received(1).ExecuteForRecover(automationInfo, Arg.Any<IEnumerable<TicketRecoverEmailModel>>());
        await VoidBetAutomationService.Received(1).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await CallSignalRStage.Received(1).ExecuteForAutoRecover(Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<VoidBetAutomationInfo>());
        await WriteLogAfterVoidStage.Received(1).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<Result>>());
    }

    [Fact]
    public async Task Execute_NotSentEmail_NotUpdateEmailResult()
    {
        // Arrange
        UpdateEmailResultStage = Substitute.For<UpdateEmailResultStage>();
        VoidBetAutomationService = Substitute.For<VoidBetAutomationService>();
        CallCashOutApiStage = Substitute.For<CallCashOutApiStage>();
        WriteLogAfterVoidStage = Substitute.For<WriteLogAfterVoidStage>();
        CallSignalRStage = Substitute.For<CallSignalRStage>();
        MainRecoverProcessStageService = Substitute.For<MainRecoverProcessStageService>();
        FakeInitDataForService(VoidBetAutomationService, UpdateEmailResultStage,
            WriteLogAfterVoidStage, CallSignalRStage,
            MainRecoverProcessStageService, CallCashOutApiStage);

        var automationInfo = new VoidBetAutomationInfo
        {
            StatusId = ExecuteStatus.New,
            VoidingHistoryId = 1,
            SiteDbGroupId = 0
        };
        var allVoidDataInCurrentVoid = new List<VoidDataXmlModel> { new() { SingleTransIds = "1, 2" } };
        var recoveredTickets = new List<TicketRecoverEmailModel>
        {
            new(),
            new()
        };
        var currentEmailResults = new List<Result>();

        VoidBetAutomationService.GetVoidDataFromVoidingHistory(1).ReturnsForAnyArgs(allVoidDataInCurrentVoid);
        VoidBetAutomationService.GetRecoveredTicketsByVoidingHistory("1, 2", automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId).ReturnsForAnyArgs(recoveredTickets);
        MainRecoverProcessStageService.DeleteZzzBetTrans(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.DeleteMessage(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.DeleteTransaction(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<BetId>>()).ReturnsForAnyArgs(true);
        MainRecoverProcessStageService.SendEmail(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketRecoverEmailModel>>()).ReturnsForAnyArgs((false, currentEmailResults));
        MainRecoverProcessStageService.UpdateTicketVoidingHistory(true, Arg.Any<UpdateTicketVoidingHistoryStageParameter>()).ReturnsForAnyArgs(true);
        FakeInitDataForService(VoidBetAutomationService, UpdateEmailResultStage, WriteLogAfterVoidStage, CallSignalRStage, MainRecoverProcessStageService, CallCashOutApiStage);

        // Act
        await Service.Execute(automationInfo, true, "");

        // Assert
        await VoidBetAutomationService.Received(1).GetVoidDataFromVoidingHistory(1);
        await VoidBetAutomationService.Received(1).GetRecoveredTicketsByVoidingHistory("1, 2", automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId);
        await MainRecoverProcessStageService.Received(1).DeleteZzzBetTrans(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>());
        await MainRecoverProcessStageService.Received(1).DeleteMessage(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<string>());
        await MainRecoverProcessStageService.Received(1).DeleteTransaction(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<BetId>>());
        await MainRecoverProcessStageService.Received(1).SendEmail(true, Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<TicketRecoverEmailModel>>());
        await UpdateEmailResultStage.Received(0).Execute(automationInfo, Arg.Any<IEnumerable<Result>>());
        await CallCashOutApiStage.Received(0).ExecuteForRecover(automationInfo, Arg.Any<IEnumerable<TicketRecoverEmailModel>>());
        await VoidBetAutomationService.Received(0).UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await CallSignalRStage.Received(0).ExecuteForManualVoid(Arg.Any<CallSignalRManualVoidParameter>());
        await WriteLogAfterVoidStage.Received(0).Execute(Arg.Any<VoidBetAutomationInfo>(), Arg.Any<IEnumerable<Result>>());
    }
}