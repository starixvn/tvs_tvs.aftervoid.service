using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface AfterVoidEmailService
    {
        Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message);
    }

    public class AfterVoidEmailServiceImp : AfterVoidEmailService
    {
        private readonly TicketVoidingSystemApi ticketVoidingSystemApi;
        private readonly IDynamicRepository repository;

        public AfterVoidEmailServiceImp(TicketVoidingSystemApi ticketVoidingSystemApi, IDynamicRepository repository)
        {
            this.ticketVoidingSystemApi = ticketVoidingSystemApi;
            this.repository = repository;
        }

        public async Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message)
        {
            if (Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId))
            {
                var subIssueType = await GetSubIssueTypeById(automationInfo.RequestInfo.EventByMatch.SubIssueTypeId);
                reason.ReasonEnglish = $"{reason.ReasonEnglish}({subIssueType.SubIssueTypeName})";
                reason.ReasonChineseSimplified = $"{reason.ReasonChineseSimplified}({subIssueType.SubIssueTypeNameCS})";
            }

            var (isSentEmail, results) = await ticketVoidingSystemApi.SendEmail(emailTickets, match, reason, message,
                automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.NegativeBalanceUsers);
            if (isSentEmail)
            {
                return results;
            }

            throw new ExternalException("ticketVoidingSystemApi.SendEmail");
        }

        public async Task<SubIssueType> GetSubIssueTypeById(int subId)
        {
            var subIssueTypes = await repository.FetchAsync<SubIssueType>(new GetSubIssueTypeCriteria());

            return subIssueTypes.FirstOrDefault(x => x.SubIssueTypeId == subId);
        }
    }
}