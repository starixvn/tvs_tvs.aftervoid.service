using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface AfterVoidEmailService
    {
        Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message);
    }

    public class AfterVoidEmailServiceImp : AfterVoidEmailService
    {
        private readonly IDynamicRepository repository;
        private readonly EmailVoidService<TicketEmailViewModel, TicketEmailModel> emailVoidService;

        public AfterVoidEmailServiceImp(IDynamicRepository repository, EmailVoidService<TicketEmailViewModel, TicketEmailModel> emailVoidService)
        {
            this.repository = repository;
            this.emailVoidService = emailVoidService;
        }

        public async Task<IEnumerable<Result>> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason reason, Message message)
        {
            var emailModel = new EmailVoidModel
            {
                Match = match,
                Message = message,
                IsLikelyAbnormalBet = Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId),
                IsAbnormalAccount = Helper.IsAbnormalAccount(automationInfo.RequestInfo.Request.IssueTypeId),
                NegativeBalanceUsers = automationInfo.NegativeBalanceUsers,
                Reason = reason.ReasonEnglish,
                ReasonSC = reason.ReasonChineseSimplified
            };

            if (emailModel.IsLikelyAbnormalBet)
            {
                var subIssueType = await GetSubIssueTypeById(automationInfo.RequestInfo.EventByMatch.SubIssueTypeId);
                emailModel.Reason = $"{reason.ReasonEnglish}({subIssueType.SubIssueTypeName})";
                emailModel.ReasonSC = $"{reason.ReasonChineseSimplified}({subIssueType.SubIssueTypeNameCS})";
            }

            return emailVoidService.SendEmail(emailModel, emailTickets.ToList());
        }

        public async Task<SubIssueType> GetSubIssueTypeById(int subId)
        {
            var subIssueTypes = await repository.FetchAsync<SubIssueType>(new GetSubIssueTypeCriteria());

            return subIssueTypes.FirstOrDefault(x => x.SubIssueTypeId == subId);
        }
    }
}