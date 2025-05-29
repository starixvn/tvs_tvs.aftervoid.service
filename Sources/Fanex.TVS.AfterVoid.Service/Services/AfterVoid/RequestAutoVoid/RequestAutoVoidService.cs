using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.RequestAutoVoid;

public interface RequestAutoVoidService
{
    Task Execute(VoidBetAutomationInfo automationInfo, bool isLastedInRequest, string currentRequestStatus);
}

public class RequestAutoVoidServiceImp(
    VoidBetAutomationService voidBetAutomationService,
    MainProcessStageService mainProcessStageService,
    UpdateEmailResultStage updateEmailResultStage,
    CallCashOutApiStage callCashOutApiStage,
    WriteLogAfterVoidStage writeLogAfterVoidStage,
    CallSignalRStage callSignalRStage,
    SpecialPlatformService specialPlatformService,
    CallMTSApiStage callMTSApiStage)
    : RequestAutoVoidService
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
        var matchInfo = await mainProcessStageService.GetMatchInfo(automationInfo.MatchId);
        var voidDataXml = await voidBetAutomationService.GetVoidDataFromVoidingHistory(automationInfo.VoidingHistoryId);
        var voidedTickets = await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(automationInfo.VoidingHistoryId, automationInfo.SiteDbGroupId, true);
        var (normalTickets, specialTickets) = specialPlatformService.GroupTicketSaba(voidedTickets);

        var (normalMessageId, specialMessageId, normalMessageContent, specialMessageContent) = await CreateMessage(automationInfo, matchInfo, voidDataXml, normalTickets, specialTickets);

        var voidReason = await CreateVoidReason(automationInfo, matchInfo, normalMessageId, specialMessageId);

        var isSendBatchMessage = await SendBatchMessage(automationInfo, voidReason, normalTickets, specialTickets, normalMessageId, specialMessageId);

        var isSentMessageToAdminSite = await mainProcessStageService.SendMessageToAdminSite(isSendBatchMessage, automationInfo, normalMessageContent);
        var isInsertedVoidedTransaction = isSentMessageToAdminSite
                                          && await InsertVoidedTransaction(automationInfo, voidReason, normalTickets, specialTickets, normalMessageId, specialMessageId);

        var isUpdatedVoidingHistory = await UpdateTicketVoidingHistory(isInsertedVoidedTransaction, automationInfo,
            voidReason, voidedTickets, normalMessageId, specialMessageId);

        var (isSentEmail, currentEmailResults) = await SendEmail(isUpdatedVoidingHistory, automationInfo, matchInfo,
            voidReason, voidedTickets, normalMessageContent, specialMessageContent);

        if (!isSentEmail)
        {
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            return;
        }

        await updateEmailResultStage.Execute(automationInfo, currentEmailResults);
        await callCashOutApiStage.ExecuteForVoid(automationInfo, voidedTickets);
        await callMTSApiStage.Execute(automationInfo, voidedTickets);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Done);
        await callSignalRStage.ExecuteForAutoVoid(isLastedInRequest, currentRequestStatus, automationInfo);
        await writeLogAfterVoidStage.Execute(automationInfo, currentEmailResults);
    }

    private static int GetSportTypeId(Match matchInfo)
    {
        return matchInfo?.SportType ?? 0;
    }

    private async Task<(int, int, Message, Message)> CreateMessage(VoidBetAutomationInfo automationInfo, Match matchInfo, VoidDataXmlModel voidDataXml,
        IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        return await mainProcessStageService.CreateMessage(
            new CreateMessageStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                VoidDataXml = voidDataXml,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets
            });
    }

    private async Task<VoidedReason> CreateVoidReason(VoidBetAutomationInfo automationInfo, Match matchInfo, int normalMessageId, int specialMessageId)
    {
        var isHaveMessage = mainProcessStageService.IsHaveMessage(normalMessageId, specialMessageId);
        return await mainProcessStageService.CreateVoidReason(isHaveMessage,
            new CreateVoidReasonStageParameter
            {
                AutomationInfo = automationInfo,
                SportTypeId = GetSportTypeId(matchInfo),
                VoidData = null
            });
    }

    private async Task<bool> SendBatchMessage(VoidBetAutomationInfo automationInfo, VoidedReason voidReason, IEnumerable<TicketEmailModel> normalTickets,
        IEnumerable<TicketEmailModel> specialTickets, int normalMessageId, int specialMessageId)
    {
        var isHaveVoidReason = mainProcessStageService.IsHaveVoidReason(voidReason);
        return await mainProcessStageService.SendBatchMessage(isHaveVoidReason,
            new SendBatchMessageStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidData = null
            });
    }

    private async Task<bool> InsertVoidedTransaction(VoidBetAutomationInfo automationInfo, VoidedReason voidReason, IEnumerable<TicketEmailModel> normalTickets,
        IEnumerable<TicketEmailModel> specialTickets, int normalMessageId, int specialMessageId)
    {
        return await mainProcessStageService.InsertVoidedTransaction(true,
            new InsertVoidedTransactionStageParameter
            {
                AutomationInfo = automationInfo,
                NormalTickets = normalTickets,
                SpecialTickets = specialTickets,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason?.ReasonId ?? 0
            });
    }

    private async Task<bool> UpdateTicketVoidingHistory(bool isInsertedVoidedTransaction, VoidBetAutomationInfo automationInfo, VoidedReason voidReason,
        IEnumerable<TicketEmailModel> voidedTickets, int normalMessageId, int specialMessageId)
    {
        return await mainProcessStageService.UpdateTicketVoidingHistory(isInsertedVoidedTransaction,
            new UpdateTicketVoidingHistoryStageParameter
            {
                AutomationInfo = automationInfo,
                NormalMessageId = normalMessageId,
                SpecialMessageId = specialMessageId,
                VoidReasonId = voidReason?.ReasonId ?? 0,
                EmailTickets = voidedTickets
            });
    }

    private async Task<(bool, IEnumerable<Result>)> SendEmail(bool isUpdatedVoidingHistory, VoidBetAutomationInfo automationInfo, Match matchInfo, VoidedReason voidReason,
        IEnumerable<TicketEmailModel> voidedTickets, Message normalMessageContent, Message specialMessageContent)
    {
        return await mainProcessStageService.SendEmail(isUpdatedVoidingHistory,
            new SendEmailStageParameter
            {
                AutomationInfo = automationInfo,
                MatchInfo = matchInfo,
                NormalMessage = normalMessageContent,
                SpecialMessage = specialMessageContent,
                VoidReason = voidReason,
                EmailTickets = voidedTickets
            });
    }
}