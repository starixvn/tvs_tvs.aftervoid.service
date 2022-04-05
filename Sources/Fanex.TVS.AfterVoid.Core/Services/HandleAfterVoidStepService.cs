using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Extensions;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface HandleAfterVoidStepService
    {
        Task<Message> CreateMessageIdForAllRequests(IEnumerable<VoidBetAutomationInfo> automationInfoList,
            Match matchInfo, int sessionId, bool isHasSettledTicket, int issueTypeId, int? filterBy);

        Task<bool> RunExternalService(VoidBetAutomationInfo automationInfo);

        Task<Message> CreateMessageIdForManualVoid(VoidBetAutomationInfo automationInfo, VoidDataXmlModel voidData);

        Task<VoidedReason> CreateVoidedReason(VoidBetAutomationInfo automationInfo, int sportType,
            VoidDataXmlModel voidData);

        Task<bool> SendBatchMessage(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets,
            int messageId, VoidDataXmlModel voidData);

        Task<bool> SendMessageToAdminSite(VoidBetAutomationInfo automationInfo, Message messageContent);

        Task<bool> InsertVoidedTransaction(VoidBetAutomationInfo automationInfo,
            IEnumerable<TicketEmailModel> emailTickets, int messageId, int reasonId);

        Task<bool> UpdateTicketVoidingHistory(VoidBetAutomationInfo automationInfo, int messageId, int reasonId);

        Task<(bool, IEnumerable<Result>)> SendEmail(VoidBetAutomationInfo automationInfo,
            IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, VoidedReason reason, Message message);

        Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results);

        Task<(bool, IEnumerable<Result>)> SendLetstalk(VoidBetAutomationInfo automationInfo,
            IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, VoidDataXmlModel voidData);

        void WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo);

        IEnumerable<Result> CreateResultList(IEnumerable<Result> sentEmailResults,
            IEnumerable<Result> sentLetstalkResults, string negativeBalanceUsers, int requestId, int matchId);
    }

    public class HandleAfterVoidStepServiceImp : HandleAfterVoidStepService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly SendMessageService sendMessageService;
        private readonly NotificationSignalRService notificationSignalRService;
        private readonly SendEmailService sendEmailService;
        private readonly NotificationLetstalkService notificationLetstalkService;
        private readonly LoggingService loggingService;

        public HandleAfterVoidStepServiceImp(VoidBetAutomationService voidBetAutomationService, SendMessageService sendMessageService,
            NotificationSignalRService notificationSignalRService, SendEmailService sendEmailService, NotificationLetstalkService notificationLetstalkService, LoggingService loggingService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.sendMessageService = sendMessageService;
            this.notificationSignalRService = notificationSignalRService;
            this.sendEmailService = sendEmailService;
            this.notificationLetstalkService = notificationLetstalkService;
            this.loggingService = loggingService;
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
                await voidBetAutomationService.UpdateServiceStatusBySession(sessionId, ExecuteStatus.WinServiceFailed);
                foreach (var service in automationInfoList)
                {
                    notificationSignalRService.NotifyFailedRequest(service.RequestId);
                }

                return null;
            }
        }

        public async Task<bool> RunExternalService(VoidBetAutomationInfo automationInfo)
        {
            try
            {
                if (automationInfo.IsRunExternalService == true)
                {
                    return true;
                }

                // TODO
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsRunExternalService = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(RunExternalService)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

                return false;
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
                    await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { MessageId = message.MessageId });
                }

                return message;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(CreateMessageIdForManualVoid)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}",
                    exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

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

                    await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { ReasonId = reason.ReasonId });
                }

                return reason;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(CreateVoidedReason)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

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
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsSentBatchMessage = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendBatchMessage)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

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
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsSentMessageToAdminSite = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendMessageToAdminSite)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}",
                    exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

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
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsInsertedVoidedTransaction = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(InsertVoidedTransaction)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}",
                    exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

                return false;
            }
        }

        public async Task<bool> UpdateTicketVoidingHistory(VoidBetAutomationInfo automationInfo, int messageId, int reasonId)
        {
            try
            {
                if (automationInfo.IsUpdatedVoidingHistory == true)
                {
                    return true;
                }

                const int voidingHistoryStatusDone = 2;
                await voidBetAutomationService.UpdateTicketVoidingHistory(automationInfo.VoidingHistory, voidingHistoryStatusDone, reasonId, messageId);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

                return true;
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(UpdateTicketVoidingHistory)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}",
                    exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

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

                var results = await sendEmailService.Execute(automationInfo, emailTickets, matchInfo, reason, message);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsSentEmail = true });

                return (true, results);
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendEmail)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

                return (false, new List<Result>());
            }
        }

        public async Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results)
        {
            var sentMailResults = results.Where(x => x.ErrorCode == ErrorCode.SendEmailToLicenseeSuccess || x.ErrorCode == ErrorCode.SendEmailToLicenseeFail);
            if (sentMailResults.Any())
            {
                await voidBetAutomationService.UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, sentMailResults.ToVoidResultsXML());
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
                    UserName = voidData.CreatorName,
                    DecidedBy = voidData.DecidedBy,
                    VoidFromTime = voidData.VoidFromTime,
                    VoidToTime = voidData.VoidToTime,
                    VoidFromDate = voidData.VoidFromDate,
                    VoidToDate = voidData.VoidToDate,
                    IsMemberTemplate = !(matchInfo?.MatchId > 0)
                };

                var isSendLetstalk = notificationLetstalkService.Send(notificationLetstalkModel, emailTickets);
                await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistory, new VoidBetAutomationInfo { IsSentLetsTalk = true });

                if (!isSendLetstalk)
                {
                    results.Add(new Result { ErrorCode = ErrorCode.SendLetstalkFail, ErrorMsg = "Failed to send message via Letstalk." });
                }

                return (true, results);
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendLetstalk)} - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId}", exception);
                await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistory, ExecuteStatus.WinServiceFailed);
                notificationSignalRService.NotifyFailedRequest(automationInfo.RequestId);

                return (false, new List<Result>());
            }
        }

        public void WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"AfterVoidDone - SessionId: {automationInfo.SessionId} - VDHistoryId: {automationInfo.VoidingHistory} - RequestId: {automationInfo.RequestId} \n");
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