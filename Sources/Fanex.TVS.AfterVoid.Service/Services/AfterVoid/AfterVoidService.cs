using System;
using System.Diagnostics;
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

            if (requestsWillRunAfterVoid == null)
            {
                return;
            }

            await UpdateRequestInfo(requestsWillRunAfterVoid);

            if (requestsWillRunAfterVoid.IsAutoVoid)
            {
                await requestAutoVoidService.Execute(requestsWillRunAfterVoid);
            }
            else
            {
                await ExecuteForManualVoid(requestsWillRunAfterVoid.RequestInfo.Request.IssueTypeId, requestsWillRunAfterVoid);
            }

            loggingService.Info(
                $"End - Session: {requestsWillRunAfterVoid.SessionId} - "
                + $"Execute Time: {stopwatch.ElapsedMilliseconds} ms - "
                + $"{DateTime.Now}"
                );
        }

        private async Task UpdateRequestInfo(VoidBetAutomationInfo automationInfo)
        {
            automationInfo.RequestInfo = await voidBetAutomationService.GetRequestById(automationInfo.RequestId, automationInfo.MatchId);
        }

        private async Task ExecuteForManualVoid(int issueTypeId, VoidBetAutomationInfo automationInfo)
        {
            if (Helper.IsArchivedTicket(issueTypeId))
            {
                await requestVoid14Service.Execute(automationInfo);
            }
            else
            {
                await requestManualVoidService.Execute(automationInfo);
            }
        }
    }
}