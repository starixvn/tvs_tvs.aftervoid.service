using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk
{
    public interface SendLetstalkService
    {
        Task Execute();
    }

    public class SendLetstalkServiceImp : SendLetstalkService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly AbnormalBetLetstalkService abnormalBetLetstalkServiceImp;
        private readonly SettledBetLetstalkService settledBetLetstalkService;
        private readonly NotificationSignalRService notificationSignalRService;
        private readonly SendLetstalkServiceStage letstalkServiceHelper;
        private readonly LoggingService loggingService;

        public SendLetstalkServiceImp(VoidBetAutomationService voidBetAutomationService,
            AbnormalBetLetstalkService abnormalBetLetstalkServiceImp,
            SettledBetLetstalkService settledBetLetstalkService,
            NotificationSignalRService notificationSignalRService,
            SendLetstalkServiceStage letstalkServiceHelper,
            LoggingService loggingService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.abnormalBetLetstalkServiceImp = abnormalBetLetstalkServiceImp;
            this.settledBetLetstalkService = settledBetLetstalkService;
            this.notificationSignalRService = notificationSignalRService;
            this.letstalkServiceHelper = letstalkServiceHelper;
            this.loggingService = loggingService;
        }

        public async Task Execute()
        {
            var automationInfoList = await letstalkServiceHelper.GetVoidBetAutomationRunLetstalk();
            if (!automationInfoList.Any())
            {
                return;
            }

            var groupRequests = automationInfoList.GroupBy(x => x.RequestId).Select(x => new GroupVoidBetAutomationInfo
            {
                RequestId = x.Key,
                VoidBetAutomationList = x
            });

            foreach (var request in groupRequests)
            {
                await RunLetstalkForSingleRequest(request);
            }
        }

        private async Task RunLetstalkForSingleRequest(GroupVoidBetAutomationInfo group)
        {
            group.RequestInfo = await voidBetAutomationService.GetRequestById(group.RequestId, group.MatchId);

            var voidedTickets = await letstalkServiceHelper.GetVoidedTickets(group.VoidBetAutomationList);
            var matchInfo = await voidBetAutomationService.GetMatchInfo(group.MatchId);
            var voidDataXmlList = await letstalkServiceHelper.GetVoidDataXml(group.VoidBetAutomationList.Select(x => x.VoidingHistoryId));

            var (isSentLetstalk, sentLetstalkResults) = await SendLetstalk(group, voidedTickets, matchInfo, voidDataXmlList);
            if (!isSentLetstalk)
            {
                await notificationSignalRService.NotifyErrorWhenExecuteAfterVoidServiceToUser(voidDataXmlList.FirstOrDefault()?.CreatorName);
            }
            letstalkServiceHelper.WriteLogLetstalk(sentLetstalkResults, group);
        }

        public async Task<(bool, IEnumerable<Result>)> SendLetstalk(GroupVoidBetAutomationInfo group,
            IEnumerable<TicketEmailModel> emailTickets, Match matchInfo, IEnumerable<VoidDataXmlModel> voidDataXmlList)
        {
            try
            {
                var results = new List<Result>();
                if (group.IsSentLetsTalk == true)
                {
                    return (true, new List<Result>());
                }

                var notificationLetstalkModel = CreateNotificationLetstalkModel(group, matchInfo, voidDataXmlList);
                var isSendLetstalkSettledBet = settledBetLetstalkService.Send(notificationLetstalkModel, emailTickets);
                var isSendLetstalkAbnormalBet = true;
                if (Helper.IsLikelyAbnormalBet(group.RequestInfo.Request.IssueTypeId))
                {
                    isSendLetstalkAbnormalBet = await abnormalBetLetstalkServiceImp.Send(notificationLetstalkModel, emailTickets);
                }

                await letstalkServiceHelper.UpdateSendLetstalkStep(group.RequestId, group.BroadcastSessionId,
                    new VoidBetAutomationInfo { IsSentLetsTalk = true });

                if (!isSendLetstalkSettledBet || !isSendLetstalkAbnormalBet)
                {
                    results.Add(new Result { ErrorCode = ErrorCode.SendLetstalkFail, ErrorMsg = "Failed to send message via Letstalk." });
                }

                return (true, results);
            }
            catch (Exception exception)
            {
                loggingService.Error(
                    $"ERROR: {nameof(SendLetstalk)} - BroadcastSessionId: {group.BroadcastSessionId} - RequestId: {group.RequestId}",
                    exception);
                await letstalkServiceHelper.UpdateServiceStatusBySessionAndRequest(
                    group.RequestId, group.BroadcastSessionId, ExecuteStatus.Failed);
                await notificationSignalRService.UpdateFailedStatusForRequest(group.RequestId);

                return (false, new List<Result>());
            }
        }

        private static NotificationLetstalkModel CreateNotificationLetstalkModel(GroupVoidBetAutomationInfo group, Match matchInfo, IEnumerable<VoidDataXmlModel> voidDataXmlList)
        {
            var model = new NotificationLetstalkModel
            {
                Match = matchInfo,
                UserName = group.RequestInfo.Request.VoidedBy,
                DecidedBy = group.RequestInfo.Request.DecidedBy,
                VoidFromTime = voidDataXmlList.Select(x => x.VoidFromTime).Select(DateTime.Parse).Min(x => x).ToString(FormatKey.LongTimeFormat),
                VoidToTime = voidDataXmlList.Select(x => x.VoidToTime).Select(DateTime.Parse).Max(x => x).ToString(FormatKey.LongTimeFormat),
                VoidFromDate = voidDataXmlList.Select(x => x.VoidFromDate).Select(DateTime.Parse).Min(x => x).ToString(FormatKey.ShortDateFormatWithSlash),
                VoidToDate = voidDataXmlList.Select(x => x.VoidToDate).Select(DateTime.Parse).Min(x => x).ToString(FormatKey.ShortDateFormatWithSlash),
                IsMemberTemplate = !(matchInfo?.MatchId > 0),
                IssueTypeId = group.RequestInfo.Request.IssueTypeId,
                FilterBy = group.RequestInfo.EventByMatch.FilterBy,
                RequestId = group.RequestId,
                RequestedBy = group.RequestInfo.Request.RequestedBy
            };

            return model;
        }
    }
}