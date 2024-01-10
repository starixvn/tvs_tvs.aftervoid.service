using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface MainRecoverProcessStageService
{
    Task<bool> DeletedMessage(VoidBetAutomationInfo automationInfo, string transIds);

    Task<bool> DeleteTransaction(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds);

    Task<(bool, IEnumerable<Result>)> SendEmail(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets);
}

public class MainRecoverProcessStageServiceImp : MainRecoverProcessStageService
{
    private readonly DeletedMessageStage deletedMessageStage;
    private readonly DeleteTransactionStage deleteTransactionStage;
    private readonly SendEmailRecoverStage sendEmailRecoverStage;

    public MainRecoverProcessStageServiceImp(DeletedMessageStage deletedMessageStage,
        DeleteTransactionStage deleteTransactionStage,
        SendEmailRecoverStage sendEmailRecoverStage)
    {
        this.deletedMessageStage = deletedMessageStage;
        this.deleteTransactionStage = deleteTransactionStage;
        this.sendEmailRecoverStage = sendEmailRecoverStage;
    }

    public async Task<bool> DeletedMessage(VoidBetAutomationInfo automationInfo, string transIds)
    {
        return !string.IsNullOrWhiteSpace(transIds) && await deletedMessageStage.Execute(automationInfo, transIds);
    }

    public async Task<bool> DeleteTransaction(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds)
    {
        if (!isContinue)
        {
            return false;
        }

        return await deleteTransactionStage.Execute(automationInfo, betIds);
    }

    public async Task<(bool, IEnumerable<Result>)> SendEmail(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets)
    {
        if (!isContinue)
        {
            return (false, new List<Result>());
        }

        return await sendEmailRecoverStage.Execute(automationInfo, tickets);
    }
}