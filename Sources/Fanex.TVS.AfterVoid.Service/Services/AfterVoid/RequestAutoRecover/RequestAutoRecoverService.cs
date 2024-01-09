using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoRecover;

public interface RequestAutoRecoverService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory);
}

public class RequestAutoRecoverServiceImp : RequestAutoRecoverService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly UpdateEmailResultStage updateEmailResultStage;
    private readonly WriteLogAfterVoidStage writeLogAfterVoidStage;
    private readonly CallSignalRStage callSignalRStage;
    private readonly MainRecoverProcessStageService mainRecoverProcessStageService;
    private readonly CallCashOutApiStage callCashOutApiStage;

    public RequestAutoRecoverServiceImp(VoidBetAutomationService voidBetAutomationService, UpdateEmailResultStage updateEmailResultStage,
        WriteLogAfterVoidStage writeLogAfterVoidStage, CallSignalRStage callSignalRStage,
        MainRecoverProcessStageService mainRecoverProcessStageService, CallCashOutApiStage callCashOutApiStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.updateEmailResultStage = updateEmailResultStage;
        this.writeLogAfterVoidStage = writeLogAfterVoidStage;
        this.callSignalRStage = callSignalRStage;
        this.mainRecoverProcessStageService = mainRecoverProcessStageService;
        this.callCashOutApiStage = callCashOutApiStage;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus,
        IEnumerable<EmailResultModel> emailResultsHistory)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await ExecuteStepByStep(automationInfo, isLastedInRequest, currentRequestStatus, emailResultsHistory);
        }
    }

    private async Task ExecuteStepByStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        var allVoidDataInCurrentVoid = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var firstVoidData = allVoidDataInCurrentVoid.First();
        var recoveredTickets = await voidBetAutomationService.GetRecoveredTicketsByVoidingHistory(firstVoidData.SingleTransIds, automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId);
        var isDeletedMessage = await mainRecoverProcessStageService.DeletedMessage(automationInfo, string.Join(",", recoveredTickets.Select(x => x.TransId)));
        var betIds = recoveredTickets.Select(x => new BetId { TransId = x.TransId.ToString(), MatchId = x.MatchId });
        var isDeleteTransaction = await mainRecoverProcessStageService.DeleteTransaction(isDeletedMessage, automationInfo, betIds);

        var (isSentEmail, currentEmailResults) = await mainRecoverProcessStageService.SendEmail(isDeleteTransaction, automationInfo, recoveredTickets);

        if (!isSentEmail)
        {
            return;
        }

        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.ExecuteForRecover(automationInfo, recoveredTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForManualVoid(new CallSignalRManualVoidParameter
        {
            AutomationInfo = automationInfo,
            IsLastedInRequest = isLastedInRequest,
            CurrentRequestStatus = currentRequestStatus,
            CreatorName = firstVoidData.CreatorName,
            CurrentEmailResults = currentEmailResults,
            EmailResultsHistory = emailResultsHistory
        });
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }
}