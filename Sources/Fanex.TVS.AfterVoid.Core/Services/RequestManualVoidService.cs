using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface RequestManualVoidService
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestManualVoidServiceImp : RequestManualVoidService
    {
        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            // TODO
        }
    }
}