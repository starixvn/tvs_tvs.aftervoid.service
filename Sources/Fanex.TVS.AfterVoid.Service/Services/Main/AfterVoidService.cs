using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Services.Main
{
    public interface AfterVoidService
    {
        Task Execute();
    }

    public class AfterVoidServiceImp : AfterVoidService
    {
        private readonly VoidBetAutomationService voidBetAutomationService;
        private readonly RequestAutoVoidService requestAutoVoidService;
        private readonly RequestManualVoidService requestManualVoidService;
        private readonly RequestVoid14Service requestVoid14Service;
        private readonly LoggingService loggingService;
        private readonly NotificationSignalRService notificationSignalRService;

        public AfterVoidServiceImp(VoidBetAutomationService voidBetAutomationService,
            RequestAutoVoidService requestAutoVoidService,
            RequestManualVoidService requestManualVoidService,
            RequestVoid14Service requestVoid14Service,
            LoggingService loggingService,
            NotificationSignalRService notificationSignalRService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.requestAutoVoidService = requestAutoVoidService;
            this.requestManualVoidService = requestManualVoidService;
            this.requestVoid14Service = requestVoid14Service;
            this.loggingService = loggingService;
            this.notificationSignalRService = notificationSignalRService;
        }

        public async Task Execute()
        {
            var stopwatch = Stopwatch.StartNew();
            var automationStatistic = await voidBetAutomationService.GetVoidBetAutomationStatistic();

            await notificationSignalRService.UpdateRunningStatusForRequests(automationStatistic.RequestsWillUpdateStatus);
            await notificationSignalRService.UpdateFailedStatusForRequests(automationStatistic.RequestsWillUpdateStatus);

            if (automationStatistic.RequestsWillRunAfterVoid == null || !automationStatistic.RequestsWillRunAfterVoid.Any())
            {
                return;
            }

            await UpdateRequestInfo(automationStatistic.RequestsWillRunAfterVoid);

            var firstRequest = automationStatistic.RequestsWillRunAfterVoid.FirstOrDefault();
            if (firstRequest.IsAutoVoid)
            {
                await requestAutoVoidService.Execute(automationStatistic.RequestsWillRunAfterVoid);
            }
            else
            {
                await ExecuteForManualVoid(firstRequest.RequestInfo.Request.IssueTypeId, automationStatistic.RequestsWillRunAfterVoid);
            }

            loggingService.Info(
                $"End - Session: {firstRequest.SessionId} - "
                + $"Total Request: {automationStatistic.RequestsWillRunAfterVoid.Count()} - "
                + $"Execute Time: {stopwatch.ElapsedMilliseconds} ms - "
                + $"{DateTime.Now}"
                );
        }

        private async Task UpdateRequestInfo(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            foreach (var automationInfo in automationInfoList)
            {
                automationInfo.RequestInfo = await voidBetAutomationService.GetRequestById(automationInfo.RequestId, automationInfo.MatchId);
            }
        }

        private async Task ExecuteForManualVoid(int issueTypeId, IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            if (Helper.IsArchivedTicket(issueTypeId))
            {
                await requestVoid14Service.Execute(automationInfoList);
            }
            else
            {
                await requestManualVoidService.Execute(automationInfoList);
            }
        }
    }
}