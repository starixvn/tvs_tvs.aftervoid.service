using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services
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
            var automationStatistic = await voidBetAutomationService.GetVoidBetAutomationStatistic();

            NotifyRunningRequests(automationStatistic.RequestsWillUpdateStatus);
            await NotifyFailedRequests(automationStatistic.RequestsWillUpdateStatus);

            if (automationStatistic.RequestsWillRunAfterVoid == null || !automationStatistic.RequestsWillRunAfterVoid.Any())
            {
                return;
            }

            foreach (var request in automationStatistic.RequestsWillRunAfterVoid)
            {
                request.RequestInfo = await voidBetAutomationService.GetRequestById(request.RequestId, request.MatchId);
            }

            var firstRequest = automationStatistic.RequestsWillRunAfterVoid.FirstOrDefault();
            if (firstRequest.IsAutoVoid)
            {
                await requestAutoVoidService.Execute(automationStatistic.RequestsWillRunAfterVoid);
            }
            else
            {
                if (Helper.IsArchivedTicket(firstRequest.RequestInfo.Request.IssueTypeId))
                {
                    await requestVoid14Service.Execute(automationStatistic.RequestsWillRunAfterVoid);
                }
                else
                {
                    await requestManualVoidService.Execute(automationStatistic.RequestsWillRunAfterVoid);
                }
            }

            loggingService.Info($"End - Session: {firstRequest.SessionId} - Total Request: {automationStatistic.RequestsWillRunAfterVoid.Count()} - " + DateTime.Now);
        }

        private void NotifyRunningRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            // TODO
        }

        private async Task NotifyFailedRequests(IEnumerable<VoidBetAutomationInfo> requestsWillUpdateStatus)
        {
            // TODO
        }
    }
}