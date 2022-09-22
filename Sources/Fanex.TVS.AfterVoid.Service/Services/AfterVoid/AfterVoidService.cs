using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid
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

        public AfterVoidServiceImp(VoidBetAutomationService voidBetAutomationService,
            RequestAutoVoidService requestAutoVoidService,
            RequestManualVoidService requestManualVoidService,
            RequestVoid14Service requestVoid14Service,
            LoggingService loggingService)
        {
            this.voidBetAutomationService = voidBetAutomationService;
            this.requestAutoVoidService = requestAutoVoidService;
            this.requestManualVoidService = requestManualVoidService;
            this.requestVoid14Service = requestVoid14Service;
            this.loggingService = loggingService;
        }

        public async Task Execute()
        {
            var stopwatch = Stopwatch.StartNew();
            var requestsWillRunAfterVoid = await voidBetAutomationService.GetVoidBetAutomationStatistic();

            if (!requestsWillRunAfterVoid.Any())
            {
                return;
            }

            await UpdateRequestInfo(requestsWillRunAfterVoid);

            var firstRequest = requestsWillRunAfterVoid.FirstOrDefault();
            if (firstRequest.IsAutoVoid)
            {
                await requestAutoVoidService.Execute(requestsWillRunAfterVoid);
            }
            else
            {
                await ExecuteForManualVoid(firstRequest.RequestInfo.Request.IssueTypeId, requestsWillRunAfterVoid);
            }

            loggingService.Info(
                $"End - Session: {firstRequest.SessionId} - "
                + $"Total Request: {requestsWillRunAfterVoid.Count()} - "
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