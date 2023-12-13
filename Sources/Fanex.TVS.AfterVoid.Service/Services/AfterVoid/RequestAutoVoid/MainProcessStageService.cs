using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;

public interface MainProcessStageService
{
    Task<(int, int, Message)> CreateMessage(CreateMessageStageParameter parameter);

    Task<VoidedReason> CreateVoidReason(int normalMessageId, int specialMessageId, CreateVoidReasonStageParameter parameter);

    Task<bool> SendBatchMessage(VoidedReason voidedReason, SendBatchMessageStageParameter parameter);

    Task<bool> SendMessageToAdminSite(bool isSendBatchMessage, VoidBetAutomationInfo automationInfo, Message normalMessageContent);

    Task<bool> InsertVoidedTransaction(bool isSentMessageToAdminSite, InsertVoidedTransactionStageParameter parameter);

    Task<bool> UpdateTicketVoidingHistory(bool isInsertedVoidedTransaction, UpdateTicketVoidingHistoryStageParameter parameter);

    Task<(bool, IEnumerable<Result>)> SendEmail(bool isUpdatedVoidingHistory, SendEmailStageParameter parameter);
}

public class MainProcessStageServiceImp : MainProcessStageService
{
    private readonly CreateMessageStage createMessageStage;
    private readonly CreateVoidReasonStage createVoidReasonStage;
    private readonly SendBatchMessageStage sendBatchMessageStage;
    private readonly SendMessageToAdminSiteStage sendMessageToAdminSiteStage;
    private readonly InsertVoidedTransactionStage insertVoidedTransactionStage;
    private readonly UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage;
    private readonly SendEmailStage sendEmailStage;

    public MainProcessStageServiceImp(CreateMessageStage createMessageStage, CreateVoidReasonStage createVoidReasonStage, SendBatchMessageStage sendBatchMessageStage,
        SendMessageToAdminSiteStage sendMessageToAdminSiteStage, InsertVoidedTransactionStage insertVoidedTransactionStage,
        UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage, SendEmailStage sendEmailStage)
    {
        this.createMessageStage = createMessageStage;
        this.createVoidReasonStage = createVoidReasonStage;
        this.sendBatchMessageStage = sendBatchMessageStage;
        this.sendMessageToAdminSiteStage = sendMessageToAdminSiteStage;
        this.insertVoidedTransactionStage = insertVoidedTransactionStage;
        this.updateTicketVoidingHistoryStage = updateTicketVoidingHistoryStage;
        this.sendEmailStage = sendEmailStage;
    }

    public async Task<(int, int, Message)> CreateMessage(CreateMessageStageParameter parameter)
    {
        return await createMessageStage.Execute(parameter);
    }

    public async Task<VoidedReason> CreateVoidReason(int normalMessageId, int specialMessageId, CreateVoidReasonStageParameter parameter)
    {
        if (normalMessageId == 0 && specialMessageId == 0)
        {
            return null;
        }

        return await createVoidReasonStage.Execute(parameter);
    }

    public async Task<bool> SendBatchMessage(VoidedReason voidedReason, SendBatchMessageStageParameter parameter)
    {
        if (Helper.IsNotHaveVoidedReason(voidedReason))
        {
            return false;
        }

        return await sendBatchMessageStage.Execute(parameter);
    }

    public async Task<bool> SendMessageToAdminSite(bool isSendBatchMessage, VoidBetAutomationInfo automationInfo, Message normalMessageContent)
    {
        if (!isSendBatchMessage)
        {
            return false;
        }

        if (!Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            return await sendMessageToAdminSiteStage.Execute(automationInfo, normalMessageContent);
        }

        return true;
    }

    public async Task<bool> InsertVoidedTransaction(bool isSentMessageToAdminSite, InsertVoidedTransactionStageParameter parameter)
    {
        if (!isSentMessageToAdminSite)
        {
            return false;
        }

        return await insertVoidedTransactionStage.Execute(parameter);
    }

    public async Task<bool> UpdateTicketVoidingHistory(bool isInsertedVoidedTransaction, UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (!isInsertedVoidedTransaction)
        {
            return false;
        }

        return await updateTicketVoidingHistoryStage.Execute(parameter);
    }

    public async Task<(bool, IEnumerable<Result>)> SendEmail(bool isUpdatedVoidingHistory, SendEmailStageParameter parameter)
    {
        if (!isUpdatedVoidingHistory)
        {
            return (false, new List<Result>());
        }

        return await sendEmailStage.Execute(parameter);
    }
}