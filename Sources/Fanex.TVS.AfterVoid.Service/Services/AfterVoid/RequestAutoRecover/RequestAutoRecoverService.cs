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
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestAutoRecoverServiceImp(
    VoidBetAutomationService voidBetAutomationService,
    UpdateEmailResultStage updateEmailResultStage,
    WriteLogAfterVoidStage writeLogAfterVoidStage,
    CallSignalRStage callSignalRStage,
    MainRecoverProcessStageService mainRecoverProcessStageService,
    CallCashOutApiStage callCashOutApiStage)
    : RequestAutoRecoverService
{
    public async Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        if (automationInfo.StatusId != ExecuteStatus.Done)
        {
            await ExecuteStepByStep(automationInfo, isLastedInRequest, currentRequestStatus);
        }
    }

    private async Task ExecuteStepByStep(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus)
    {
        var recoveredTickets = await voidBetAutomationService.GetRecoveredTicketsByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId);
        var betIds = recoveredTickets.Select(x => new BetId { TransId = x.TransId.ToString(), MatchId = x.MatchId });
        var isDeletedZzzBetTrans = await mainRecoverProcessStageService.DeleteZzzBetTrans(automationInfo, string.Join(",", recoveredTickets.Select(x => x.SubTransId)));
        var isDeletedMessage = await mainRecoverProcessStageService.DeleteMessage(isDeletedZzzBetTrans, automationInfo, string.Join(",", recoveredTickets.Select(x => x.TransId)));
        var isDeleteTransaction = await mainRecoverProcessStageService.DeleteTransaction(isDeletedMessage, automationInfo, betIds);
        var isUpdatedVoidingHistory = await mainRecoverProcessStageService.UpdateTicketVoidingHistory(isDeleteTransaction,
            new UpdateTicketVoidingHistoryStageParameter
            {
                AutomationInfo = automationInfo
            });

        var (isSentEmail, currentEmailResults) = await mainRecoverProcessStageService.SendEmail(isUpdatedVoidingHistory, automationInfo, recoveredTickets);

        if (!isSentEmail)
        {
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            return;
        }

        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.ExecuteForRecover(automationInfo, recoveredTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForAutoRecover(isLastedInRequest, currentRequestStatus, automationInfo);
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }
}