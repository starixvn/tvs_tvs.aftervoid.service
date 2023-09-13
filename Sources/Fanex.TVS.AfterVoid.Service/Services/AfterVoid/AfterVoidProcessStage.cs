using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Resources;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface AfterVoidProcessStage
{
    Task<Message> CreateMessageIdForAutoVoid(VoidBetAutomationInfo automationInfo,
        Match matchInfo, string broadcastSessionId, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes, int issueTypeId, int? filterBy);

    Task<bool> RunExternalService(VoidBetAutomationInfo automationInfo);

    Task CallCashOutApi(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets);

    Task<Message> CreateMessageIdForManualVoid(VoidBetAutomationInfo automationInfo, VoidDataXmlModel voidData);

    Task<VoidedReason> CreateVoidedReason(VoidBetAutomationInfo automationInfo, int sportType,
        VoidDataXmlModel voidData);

    Task<bool> SendBatchMessage(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets,
        int messageId, VoidDataXmlModel voidData);

    Task<bool> SendMessageToAdminSite(VoidBetAutomationInfo automationInfo, Message messageContent);

    Task<bool> InsertVoidedTransaction(VoidBetAutomationInfo automationInfo,
        IEnumerable<TicketEmailModel> emailTickets, int messageId, int reasonId);

    Task<bool> UpdateTicketVoidingHistory(VoidBetAutomationInfo automationInfo, int messageId, int reasonId, IEnumerable<TicketEmailModel> emailTickets);

    Task<(bool, IEnumerable<Result>)> SendEmail(VoidBetAutomationInfo automationInfo,
        IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, VoidedReason reason, Message message);

    Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults);

    Task NotifyVoidResultsSignalR(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults,
        string creatorName, IEnumerable<EmailResultModel> emailResultsHistory);

    Task WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo);
}

public class AfterVoidProcessStageImp : AfterVoidProcessStage
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly SendMessageService sendMessageService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly AfterVoidEmailService afterVoidEmailService;
    private readonly LoggingService loggingService;
    private readonly CashOutHttpClientService cashOutHttpClientService;
    private readonly SendLetstalkServiceStage sendLetstalkServiceStage;

    public AfterVoidProcessStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        AfterVoidEmailService afterVoidEmailService,
        LoggingService loggingService,
        CashOutHttpClientService cashOutHttpClientService,
        SendLetstalkServiceStage sendLetstalkServiceStage)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.afterVoidEmailService = afterVoidEmailService;
        this.loggingService = loggingService;
        this.cashOutHttpClientService = cashOutHttpClientService;
        this.sendLetstalkServiceStage = sendLetstalkServiceStage;
    }

    public async Task<Message> CreateMessageIdForAutoVoid(VoidBetAutomationInfo automationInfo, Match matchInfo, string broadcastSessionId,
        IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes, int issueTypeId, int? filterBy)
    {
        try
        {
            Message message;
            if (automationInfo.MessageId != null && automationInfo.MessageId != 0)
            {
                message = await sendMessageService.GetMessage(automationInfo.MessageId.Value);
            }
            else
            {
                message = await sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, allVoidDataInCurrentVoidTimes, issueTypeId, filterBy, automationInfo.RequestInfo.PlayerNamesSelected);
                message.MessageId = await sendMessageService.InsertMessage(message, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, issueTypeId);
            }

            await voidBetAutomationService.UpdateMessageIdByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, message.MessageId);

            return message;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync($"ERROR: {nameof(CreateMessageIdForAutoVoid)} - BroadcastSessionId: {broadcastSessionId}", exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await sendLetstalkServiceStage.InsertLetstalk(automationInfo.RequestId, automationInfo.BroadcastSessionId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return null;
        }
    }

    [ExcludeFromCodeCoverage]
    public async Task<bool> RunExternalService(VoidBetAutomationInfo automationInfo)
    {
        try
        {
            if (automationInfo.IsRunExternalService == true)
            {
                return true;
            }

            // TODO
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsRunExternalService = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(RunExternalService)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }

    public async Task CallCashOutApi(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets)
    {
        try
        {
            if (automationInfo.IsRunExternalService == true)
            {
                return;
            }
            var basketballCashOutTickets = emailTickets.Where(x => x.IsCashOut && x.SportType == SportTypeConstant.Basketball);
            if (!basketballCashOutTickets.Any())
            {
                return;
            }
            var requestModel = new CashOutApiModel { VoidBetInfos = basketballCashOutTickets.Select(x => new VoidBetInfo { TransId = x.TransId.ToString(), CustId = x.CustId.ToString() }) };
            await cashOutHttpClientService.VoidBet(requestModel);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsRunExternalService = true });
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(CallCashOutApi)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
        }
    }

    public async Task<Message> CreateMessageIdForManualVoid(VoidBetAutomationInfo automationInfo, VoidDataXmlModel voidData)
    {
        try
        {
            Message message;
            if (automationInfo.MessageId > 0)
            {
                message = await sendMessageService.GetMessage(automationInfo.MessageId.Value);
            }
            else
            {
                var matchInfo = automationInfo.MatchId > 0 ? await voidBetAutomationService.GetMatchInfo(automationInfo.MatchId) : null;

                message = sendMessageService.InitializeMessageContentForManualVoid(voidData);
                message.MessageId = await sendMessageService.InsertMessage(message, automationInfo.MatchId, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { MessageId = message.MessageId });
            }

            return message;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(CreateMessageIdForManualVoid)} - SessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return null;
        }
    }

    public async Task<VoidedReason> CreateVoidedReason(VoidBetAutomationInfo automationInfo, int sportType, VoidDataXmlModel voidData)
    {
        try
        {
            VoidedReason reason;
            if (automationInfo.ReasonId > 0)
            {
                reason = await sendMessageService.GetVoidedReason(automationInfo.ReasonId.Value);
            }
            else
            {
                reason = await sendMessageService.InitializeVoidReason(automationInfo.IsAutoVoid, automationInfo.RequestInfo.EventByMatch.SubIssueTypeId, sportType, voidData);
                reason.ReasonId = await sendMessageService.InsertReason(reason.ReasonEnglish, reason.ReasonChineseSimplified);

                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { ReasonId = reason.ReasonId });
            }

            return reason;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(CreateVoidedReason)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return null;
        }
    }

    public async Task<bool> SendBatchMessage(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, int messageId, VoidDataXmlModel voidData)
    {
        try
        {
            if (automationInfo.IsSentBatchMessage == true)
            {
                return true;
            }

            var creatorId = automationInfo.IsAutoVoid ? AutoVoidConstant.ActorId : voidData.CreatorId;
            await sendMessageService.SendMessage(emailTickets, messageId, automationInfo.RequestInfo.Request.IssueTypeId, creatorId);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentBatchMessage = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(SendBatchMessage)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }

    public async Task<bool> SendMessageToAdminSite(VoidBetAutomationInfo automationInfo, Message messageContent)
    {
        try
        {
            if (automationInfo.IsSentMessageToAdminSite == true)
            {
                return true;
            }

            await sendMessageService.InsertMessageIntoAdminSite(messageContent);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentMessageToAdminSite = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(SendMessageToAdminSite)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }

    public async Task<bool> InsertVoidedTransaction(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, int messageId, int reasonId)
    {
        try
        {
            if (automationInfo.IsInsertedVoidedTransaction == true)
            {
                return true;
            }

            var voidedTicket = emailTickets.Where(x => !x.IsCancelCashOut);
            await sendMessageService.InsertVoidedTransaction(voidedTicket, messageId, reasonId,
                automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.RequestInfo.EventByMatch.SubIssueTypeId, automationInfo.MatchId);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsInsertedVoidedTransaction = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(InsertVoidedTransaction)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }

    public async Task<bool> UpdateTicketVoidingHistory(VoidBetAutomationInfo automationInfo, int messageId, int reasonId, IEnumerable<TicketEmailModel> emailTickets)
    {
        try
        {
            if (automationInfo.IsUpdatedVoidingHistory == true)
            {
                return true;
            }

            var transIds = Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId)
                ? string.Join(",", emailTickets.Select(x => x.TransId))
                : null;
            var minWinLostDate = GetMinWinLostDateTickets(automationInfo.RequestInfo.Request.IssueTypeId, emailTickets);
            var maxWinLostDate = GetMaxWinLostDateTickets(automationInfo.RequestInfo.Request.IssueTypeId, emailTickets);
            await voidBetAutomationService.UpdateTicketVoidingHistory(automationInfo.VoidingHistoryId, ExecuteStatus.Done, reasonId, messageId, transIds, minWinLostDate, maxWinLostDate);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(UpdateTicketVoidingHistory)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }

    public async Task<(bool, IEnumerable<Result>)> SendEmail(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets,
        Match matchInfo, VoidedReason reason, Message message)
    {
        try
        {
            if (automationInfo.IsSentEmail == true)
            {
                return (true, new List<Result>());
            }

            var results = await afterVoidEmailService.Execute(automationInfo, emailTickets, matchInfo, reason, message);
            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentEmail = true });

            return (true, results);
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(SendEmail)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return (false, new List<Result>());
        }
    }

    public async Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults)
    {
        var licenseeEmailResult = currentEmailResults.Where(x =>
            x.ErrorCode is ErrorCode.SendEmailToLicenseeSuccess or ErrorCode.SendEmailToLicenseeFail
                or ErrorCode.LicenseeSuccess or ErrorCode.LicenseeFail);
        if (licenseeEmailResult.Any())
        {
            await voidBetAutomationService.UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, licenseeEmailResult.ToVoidResultsXML());
        }
    }

    public async Task NotifyVoidResultsSignalR(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults, string creatorName, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        var signalRResults = new List<Result>();
        var successfulLicensees = GetLicenseeResult(currentEmailResults, emailResultsHistory, ErrorCode.LicenseeSuccess);
        var failedLicensees = GetLicenseeResult(currentEmailResults, emailResultsHistory, ErrorCode.LicenseeFail);
        signalRResults.Insert(0, new Result { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = GetMessageVoidRequestSuccess(automationInfo.RequestId, automationInfo.MatchId) });
        if (successfulLicensees.Any())
        {
            signalRResults.Add(new Result
            {
                ErrorCode = ErrorCode.LicenseeSuccess,
                ErrorMsg = string.Format(Labels.VoidBetSendMailSuccess, string.Join(", ", successfulLicensees.Distinct()))
            });
        }
        if (failedLicensees.Any())
        {
            signalRResults.Add(new Result
            {
                ErrorCode = ErrorCode.LicenseeFail,
                ErrorMsg = string.Format(Labels.VoidBetSendMailFail, string.Join(", ", failedLicensees.Distinct()))
            });
        }

        await notificationSignalRService.NotifyVoidResultToUser(creatorName, signalRResults);
    }

    private static IEnumerable<string> GetLicenseeResult(IEnumerable<Result> currentEmailResults, IEnumerable<EmailResultModel> emailResultsHistory, int type)
    {
        var licensees = new List<string>();
        licensees.AddRange(currentEmailResults.Where(x => x.ErrorCode == type).Select(x => x.Data.ToString()));
        licensees.AddRange(emailResultsHistory.Where(x => x.Type == type).Select(x => x.Licensees));

        return licensees;
    }

    public async Task WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo)
    {
        var logBuilder = new StringBuilder();
        logBuilder.Append($"AfterVoidDone - BroadcastSessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId} \n");
        foreach (var result in results)
        {
            var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
            logBuilder.Append(log);
        }

        await loggingService.InfoAsync(logBuilder.ToString());
    }

    private static string GetMessageVoidRequestSuccess(int requestId, int matchId)
    {
        return matchId > 0 ? $"Request ID: {requestId} Match ID: {matchId} voided successfully!" : $"Request ID: {requestId} voided successfully!";
    }

    private static DateTime? GetMinWinLostDateTickets(int issueTypeId, IEnumerable<TicketEmailModel> emailTickets)
    {
        if (Helper.IsAbnormalAccount(issueTypeId) && emailTickets.Any())
        {
            return emailTickets.Min(x => x.WinLostDate);
        }

        return null;
    }

    private static DateTime? GetMaxWinLostDateTickets(int issueTypeId, IEnumerable<TicketEmailModel> emailTickets)
    {
        if (Helper.IsAbnormalAccount(issueTypeId) && emailTickets.Any())
        {
            return emailTickets.Max(x => x.WinLostDate);
        }

        return null;
    }
}