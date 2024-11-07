using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface MainProcessStageService
{
    Task<(int, int, Message, Message)> CreateMessage(CreateMessageStageParameter parameter);

    bool IsHaveMessage(int normalMessageId, int specialMessageId);

    Task<VoidedReason> CreateVoidReason(bool isContinue, CreateVoidReasonStageParameter parameter);

    bool IsHaveVoidReason(VoidedReason voidedReason);

    Task<bool> SendBatchMessage(bool isContinue, SendBatchMessageStageParameter parameter);

    Task<bool> SendMessageToAdminSite(bool isContinue, VoidBetAutomationInfo automationInfo, Message normalMessageContent);

    Task<bool> InsertVoidedTransaction(bool isContinue, InsertVoidedTransactionStageParameter parameter);

    Task<bool> UpdateTicketVoidingHistory(bool isContinue, UpdateTicketVoidingHistoryStageParameter parameter);

    Task<(bool, IEnumerable<Result>)> SendEmail(bool isContinue, SendEmailStageParameter parameter);

    Task<Match> GetMatchInfo(int matchId);
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
    private readonly VoidBetAutomationService voidBetAutomationService;

    public MainProcessStageServiceImp(CreateMessageStage createMessageStage, CreateVoidReasonStage createVoidReasonStage, SendBatchMessageStage sendBatchMessageStage,
        SendMessageToAdminSiteStage sendMessageToAdminSiteStage, InsertVoidedTransactionStage insertVoidedTransactionStage,
        UpdateTicketVoidingHistoryStage updateTicketVoidingHistoryStage, SendEmailStage sendEmailStage, VoidBetAutomationService voidBetAutomationService)
    {
        this.createMessageStage = createMessageStage;
        this.createVoidReasonStage = createVoidReasonStage;
        this.sendBatchMessageStage = sendBatchMessageStage;
        this.sendMessageToAdminSiteStage = sendMessageToAdminSiteStage;
        this.insertVoidedTransactionStage = insertVoidedTransactionStage;
        this.updateTicketVoidingHistoryStage = updateTicketVoidingHistoryStage;
        this.sendEmailStage = sendEmailStage;
        this.voidBetAutomationService = voidBetAutomationService;
    }

    public async Task<(int, int, Message, Message)> CreateMessage(CreateMessageStageParameter parameter)
    {
        return await createMessageStage.Execute(parameter);
    }

    public bool IsHaveMessage(int normalMessageId, int specialMessageId)
    {
        return normalMessageId != 0 || specialMessageId != 0;
    }

    public async Task<VoidedReason> CreateVoidReason(bool isContinue, CreateVoidReasonStageParameter parameter)
    {
        if (isContinue)
        {
            return await createVoidReasonStage.Execute(parameter);
        }

        return null;
    }

    public bool IsHaveVoidReason(VoidedReason voidedReason)
    {
        return voidedReason is { ReasonId: > 0 };
    }

    public async Task<bool> SendBatchMessage(bool isContinue, SendBatchMessageStageParameter parameter)
    {
        if (isContinue)
        {
            return await sendBatchMessageStage.Execute(parameter);
        }

        return false;
    }

    public async Task<bool> SendMessageToAdminSite(bool isContinue, VoidBetAutomationInfo automationInfo, Message normalMessageContent)
    {
        if (!isContinue)
        {
            return false;
        }

        if (!Helper.IsAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
        {
            return await sendMessageToAdminSiteStage.Execute(automationInfo, normalMessageContent);
        }

        return true;
    }

    public async Task<bool> InsertVoidedTransaction(bool isContinue, InsertVoidedTransactionStageParameter parameter)
    {
        if (isContinue)
        {
            return await insertVoidedTransactionStage.Execute(parameter);
        }

        return false;
    }

    public async Task<bool> UpdateTicketVoidingHistory(bool isContinue, UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (isContinue)
        {
            return await updateTicketVoidingHistoryStage.Execute(parameter);
        }

        return false;
    }

    public async Task<(bool, IEnumerable<Result>)> SendEmail(bool isContinue, SendEmailStageParameter parameter)
    {
        if (isContinue)
        {
            return await sendEmailStage.Execute(parameter);
        }

        return (false, new List<Result>());
    }

    public async Task<Match> GetMatchInfo(int matchId)
    {
        return matchId > 0 ? await voidBetAutomationService.GetMatchInfo(matchId) : null;
    }
}