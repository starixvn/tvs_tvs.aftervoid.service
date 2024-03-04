using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface MainRecoverProcessStageService
{
    Task<bool> DeleteZzzBetTrans(VoidBetAutomationInfo automationInfo, string transIds);

    Task<bool> DeleteMessage(bool isContinue, VoidBetAutomationInfo automationInfo, string transIds);

    Task<bool> DeleteTransaction(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<BetId> betIds);

    Task<bool> UpdateTicketVoidingHistory(bool isContinue, UpdateTicketVoidingHistoryStageParameter parameter);

    Task<(bool, IEnumerable<Result>)> SendEmail(bool isContinue, VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets);
}

public class MainRecoverProcessStageServiceImp : MainRecoverProcessStageService
{
    private readonly DeleteZzzBetTransStage deleteZzzBetTransStage;
    private readonly DeleteMessageStage deletedMessageStage;
    private readonly DeleteTransactionStage deleteTransactionStage;
    private readonly SendEmailRecoverStage sendEmailRecoverStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;

    public MainRecoverProcessStageServiceImp(DeleteMessageStage deletedMessageStage, DeleteTransactionStage deleteTransactionStage,
        SendEmailRecoverStage sendEmailRecoverStage, UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage, DeleteZzzBetTransStage deleteZzzBetTransStage)
    {
        this.deleteZzzBetTransStage = deleteZzzBetTransStage;
        this.deletedMessageStage = deletedMessageStage;
        this.deleteTransactionStage = deleteTransactionStage;
        this.sendEmailRecoverStage = sendEmailRecoverStage;
        this.updateTicketVoidingHistoryStage = updateTicketVoidingHistoryStage;
    }

    public async Task<bool> DeleteZzzBetTrans(VoidBetAutomationInfo automationInfo, string transIds)
    {
        return !string.IsNullOrWhiteSpace(transIds) && await deleteZzzBetTransStage.Execute(automationInfo, transIds);
    }

    public async Task<bool> DeleteMessage(bool isContinue, VoidBetAutomationInfo automationInfo, string transIds)
    {
        if (!isContinue)
        {
            return false;
        }

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

    public async Task<bool> UpdateTicketVoidingHistory(bool isContinue, UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (isContinue)
        {
            return await updateTicketVoidingHistoryStage.Execute(parameter);
        }

        return false;
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