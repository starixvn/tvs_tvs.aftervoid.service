using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface RequestVoid14Service
    {
        Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList);
    }

    public class RequestVoid14ServiceImp : RequestVoid14Service
    {
        public async Task Execute(IEnumerable<VoidBetAutomationInfo> automationInfoList)
        {
            // TODO
        }
    }
}