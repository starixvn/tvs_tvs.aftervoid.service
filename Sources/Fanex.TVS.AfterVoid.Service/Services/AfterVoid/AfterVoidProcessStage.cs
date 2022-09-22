using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
{
    public interface AfterVoidProcessStage
    {
        Task<Message> CreateMessageIdForAllRequests(IEnumerable<VoidBetAutomationInfo> automationInfoList,
            Match matchInfo, int sessionId, bool isHasSettledTicket, int issueTypeId, int? filterBy);

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

        Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results);

        Task<(bool, IEnumerable<Result>)> SendLetstalk(VoidBetAutomationInfo automationInfo,
            IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, VoidDataXmlModel voidData);

        void WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo);

        IEnumerable<Result> CreateResultList(IEnumerable<Result> sentEmailResults,
            IEnumerable<Result> sentLetstalkResults, string negativeBalanceUsers, int requestId, int matchId);
    }

    public class AfterVoidProcessStageImp : AfterVoidProcessStage
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly SendMessageService sendMessageService;
        private readonly NotificationSignalRService notificationSignalRService;
        private readonly AfterVoidEmailService afterVoidEmailService;
        private readonly NotificationLetstalkService notificationLetstalkService;
        private readonly LoggingService loggingService;
        private readonly CashOutHttpClientService cashOutHttpClientService;

        public AfterVoidProcessStageImp(VoidBetAutomationService voidBetAutomationService, SendMessageService sendMessageService,
            NotificationSignalRService notificationSignalRService, AfterVoidEmailService afterVoidEmailService,
            NotificationLetstalkService notificationLetstalkService, LoggingService loggingService, CashOutHttpClientService cashOutHttpClientService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.sendMessageService = sendMessageService;
            this.notificationSignalRService = notificationSignalRService;
            this.afterVoidEmailService = afterVoidEmailService;
            this.notificationLetstalkService = notificationLetstalkService;
            this.loggingService = loggingService;
            this.cashOutHttpClientService = cashOutHttpClientService;
        }

        public async Task<Message> CreateMessageIdForAllRequests(IEnumerable<VoidBetAutomationInfo> automationInfoList, Match matchInfo, int sessionId,
            bool isHasSettledTicket, int issueTypeId, int? filterBy)
        {
            try
            {
                Message message;
                var serviceInfoHasMessageId = automationInfoList.FirstOrDefault(x => x.MessageId != null && x.MessageId != 0);
                if (serviceInfoHasMessageId != null)
                {
                    message = await sendMessageService.GetMessage(serviceInfoHasMessageId.MessageId.Value);
                }
                else
                {
                    message = await sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, isHasSettledTicket, issueTypeId, filterBy);
                    message.MessageId = await sendMessageService.InsertMessage(message, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, issueTypeId);
                }

                await voidBetAutomationService.UpdateMessageIdBySession(sessionId, message.MessageId);

                return message;
            }
            catch (Exception exception)
            {
                loggingService.Error($"ERROR: {nameof(CreateMessageIdForAllRequests)} - SessionId: {sessionId}", exception);
                await voidBetAutomationService.UpdateServiceStatusBySession(sessionId, ExecuteStatus.Failed);
                foreach (var service in automationInfoList)
                {
                    await notificationSignalRService.UpdateFailedStatusForRequest(service.RequestId);
                }

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
                loggingService.Error(
                    $"ERROR: {nameof(RunExternalService)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
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
                loggingService.Error(
                    $"ERROR: {nameof(CallCashOutApi)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
            }
        }

        public async Task<Message> CreateMessageIdForManualVoid(VoidBetAutomationInfo automationInfo, VoidDataXmlModel voidData)
        {
            try
            {
                Message message;
                if (automationInfo.MessageId != null)
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
                loggingService.Error(
                    $"ERROR: {nameof(CreateMessageIdForManualVoid)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
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
                loggingService.Error(
                    $"ERROR: {nameof(CreateVoidedReason)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
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
                loggingService.Error(
                    $"ERROR: {nameof(SendBatchMessage)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
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
                loggingService.Error(
                    $"ERROR: {nameof(SendMessageToAdminSite)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
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

                await sendMessageService.InsertVoidedTransaction(emailTickets, messageId, reasonId,
                    automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.RequestInfo.EventByMatch.SubIssueTypeId, automationInfo.MatchId);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsInsertedVoidedTransaction = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(InsertVoidedTransaction)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
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
                DateTime? minWinLostDate = Helper.IsAbnormalAccount(automationInfo.RequestInfo.Request.IssueTypeId) ? emailTickets.Min(x => x.WinLostDate) : null;
                DateTime? maxWinLostDate = Helper.IsAbnormalAccount(automationInfo.RequestInfo.Request.IssueTypeId) ? emailTickets.Max(x => x.WinLostDate) : null;
                await voidBetAutomationService.UpdateTicketVoidingHistory(automationInfo.VoidingHistoryId, ExecuteStatus.Done, reasonId, messageId, transIds, minWinLostDate, maxWinLostDate);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(UpdateTicketVoidingHistory)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
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
                loggingService.Error(
                    $"ERROR: {nameof(SendEmail)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
                await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

                return (false, new List<Result>());
            }
        }

        public async Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results)
        {
            var sentMailResults = results.Where(x => x.ErrorCode == ErrorCode.SendEmailToLicenseeSuccess || x.ErrorCode == ErrorCode.SendEmailToLicenseeFail);
            if (sentMailResults.Any())
            {
                await voidBetAutomationService.UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, sentMailResults.ToVoidResultsXML());
            }
        }

        public async Task<(bool, IEnumerable<Result>)> SendLetstalk(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, VoidDataXmlModel voidData)
        {
            try
            {
                var results = new List<Result>();
                if (automationInfo.IsSentLetsTalk == true)
                {
                    return (true, new List<Result>());
                }

                var notificationLetstalkModel = new NotificationLetstalkModel
                {
                    Match = matchInfo,
                    UserName = automationInfo.RequestInfo.Request.VoidedBy,
                    DecidedBy = automationInfo.RequestInfo.Request.DecidedBy,
                    VoidFromTime = voidData.VoidFromTime,
                    VoidToTime = voidData.VoidToTime,
                    VoidFromDate = voidData.VoidFromDate,
                    VoidToDate = voidData.VoidToDate,
                    IsMemberTemplate = !(matchInfo?.MatchId > 0),
                    IssueTypeId = automationInfo.RequestInfo.Request.IssueTypeId,
                    FilterBy = automationInfo.RequestInfo.EventByMatch.FilterBy,
                    RequestId = automationInfo.RequestId,
                    RequestedBy = automationInfo.RequestInfo.Request.RequestedBy,
                    VoidingHistoryId = automationInfo.VoidingHistoryId
                };

                var isSendLetstalkSettledBet = notificationLetstalkService.SendToSettledBetGroup(notificationLetstalkModel, emailTickets);
                var isSendLetstalkAbnormalBet = true;
                if (Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
                {
                    isSendLetstalkAbnormalBet = await notificationLetstalkService.SendToAbnormalBetGroup(notificationLetstalkModel, emailTickets);
                }

                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentLetsTalk = true });

                if (!isSendLetstalkSettledBet || !isSendLetstalkAbnormalBet)
                {
                    results.Add(new Result { ErrorCode = ErrorCode.SendLetstalkFail, ErrorMsg = "Failed to send message via Letstalk." });
                }

                return (true, results);
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendLetstalk)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
                await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

                return (false, new List<Result>());
            }
        }

        public void WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"AfterVoidDone - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId} \n");
            foreach (var result in results)
            {
                var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
                logBuilder.Append(log);
            }

            loggingService.Info(logBuilder.ToString());
        }

        public IEnumerable<Result> CreateResultList(IEnumerable<Result> sentEmailResults, IEnumerable<Result> sentLetstalkResults, string negativeBalanceUsers, int requestId, int matchId)
        {
            var results = new List<Result>();
            results.AddRange(sentEmailResults ?? new List<Result>());
            results.AddRange(sentLetstalkResults ?? new List<Result>());
            if (!string.IsNullOrEmpty(negativeBalanceUsers))
            {
                results.Add(new Result { ErrorCode = ErrorCode.NegativeBalanceUsers, ErrorMsg = negativeBalanceUsers });
            }
            results.Insert(0, new Result { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = GetMessageVoidRequestSuccess(requestId, matchId) });

            return results;
        }

        private static string GetMessageVoidRequestSuccess(int requestId, int matchId)
        {
            return matchId > 0 ? $"Request ID: {requestId} Match ID: {matchId} voided successfully!" : $"Request ID: {requestId} voided successfully!";
        }
    }
}