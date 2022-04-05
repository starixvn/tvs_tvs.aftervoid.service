using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface NotificationSignalRService
    {
        void NotifyFailedRequest(int requestId);

        Task NotifyFailedRequests(IEnumerable<VoidBetAutomationInfo> automationInfoList);

        void NotifyException(string userName);

        Task NotifyCompletedRequests(int requestId, int voidingHistoryId);

        void NotifyVoidResult(IEnumerable<Result> results, string userName);
    }

    public class NotificationSignalRServiceImp : NotificationSignalRService
    {
        public void NotifyFailedRequest(int requestId)
        {
            throw new NotImplementedException();
        }

        public Task NotifyFailedRequests(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            throw new NotImplementedException();
        }

        public void NotifyException(string userName)
        {
            throw new NotImplementedException();
        }

        public Task NotifyCompletedRequests(int requestId, int voidingHistoryId)
        {
            throw new NotImplementedException();
        }

        public void NotifyVoidResult(IEnumerable<Result> results, string userName)
        {
            throw new NotImplementedException();
        }
    }
}