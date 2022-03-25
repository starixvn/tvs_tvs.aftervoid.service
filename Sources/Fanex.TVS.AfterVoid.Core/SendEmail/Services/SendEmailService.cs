using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface SendEmailService
    {
        Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message);
    }

    public class SendEmailServiceImp : SendEmailService
    {
        public Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason,
            Message message)
        {
            throw new System.NotImplementedException();
        }
    }
}