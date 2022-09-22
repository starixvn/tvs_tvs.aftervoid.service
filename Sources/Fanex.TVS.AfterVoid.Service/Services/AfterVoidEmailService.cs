using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services
{
    public interface AfterVoidEmailService
    {
        Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message);

        Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, int voidingHistoryId);
    }

    public class AfterVoidEmailServiceImp : AfterVoidEmailService
    {
        private readonly TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService;
        private readonly IDynamicRepository repository;

        public AfterVoidEmailServiceImp(TicketVoidingSystemHttpClientService ticketVoidingSystemHttpClientService, IDynamicRepository repository)
        {
            this.ticketVoidingSystemHttpClientService = ticketVoidingSystemHttpClientService;
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

            var (isSentEmail, results) = await ticketVoidingSystemHttpClientService.SendEmail(emailTickets, match, reason, message,
                automationInfo.RequestInfo.Request.IssueTypeId, automationInfo.NegativeBalanceUsers);
            if (isSentEmail)
            {
                return results;
            }

            throw new ExternalException("ticketVoidingSystemHttpClientService.SendEmail");
        }

        public async Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, int voidingHistoryId)
        {
            var (isSentEmail, results) = await ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket(requestId, matchId, issueTypeId, voidingHistoryId.ToString());
            if (isSentEmail)
            {
                return results;
            }

            throw new ExternalException("ticketVoidingSystemHttpClientService.GetContentExcelVoidedTicket");
        }

        private async Task<SubIssueType> GetSubIssueTypeById(int subId)
        {
            var subIssueTypes = await repository.FetchAsync<SubIssueType>(new GetSubIssueTypeCriteria());

            return subIssueTypes.FirstOrDefault(x => x.SubIssueTypeId == subId);
        }
    }
}