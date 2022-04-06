using System.Collections.Generic;
using System.Linq;
using Fanex.Data.Repository;
using Fanex.MailDeliver.Model;
using Fanex.TVS.AfterVoid.Core.Criteria;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Extensions;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Services.Subject;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Void
{
    public class RecordService : RecordVoidService<TicketEmailViewModel, TicketEmailModel>
    {
        private readonly SubjectService subjectService;
        private readonly ITicketService ticketService;
        private readonly TemplateEmailService templateEmailService;
        private readonly IDynamicRepository repository;

        public RecordService(
            SubjectService subjectService,
            ITicketService ticketService,
            TemplateEmailService templateEmailService,
            IDynamicRepository repository)
        {
            this.subjectService = subjectService;
            this.ticketService = ticketService;
            this.templateEmailService = templateEmailService;
            this.repository = repository;
        }

        public MailRecord Create(MailVoidModel<TicketEmailViewModel, TicketEmailModel> model, EmailGroup group)
        {
            if (model.Valid())
            {
                model.ViewModel.NegativeBalanceUsers = model.GetNegativeBalanceUsers(group, t => t.UserName);
                model.ViewModel.Tickets = AssignTickets(model.Tickets, model.ViewModel.Match, model.Site);

                var voidType = model.ViewModel.IsLikelyAbnormalBet || model.ViewModel.IsAbnormalAccount ? VoidType.Abnormal : VoidType.None;
                var subject = subjectService.CreateVoid(model, group, voidType);
                var mailRecord = templateEmailService.Create(model, subject);

                return mailRecord.MailRecord;
            }

            return new MailRecord();
        }

        private IEnumerable<TicketEmailViewModel> AssignTickets(IEnumerable<TicketEmailModel> ticketModels, Match match, SiteEmail site)
        {
            var tickets = new List<TicketEmailViewModel>();
            var index = 1;
            var operators = GetOperators(ticketModels);
            foreach (var item in ticketModels)
            {
                item.OperatorID = operators.FirstOrDefault(x => x.AgentId == item.Recommend).OperatorID;
                var ticket = ticketService.GetTicket(item, match, site);
                ticket.No = index++;
                tickets.Add(ticket);
            }

            return tickets;
        }

        private IEnumerable<GetOperatorInfoModel> GetOperators(IEnumerable<TicketEmailModel> ticketModels)
        {
            var criteria = new GetOperatorInfoCriteria { AgentIdsXml = ticketModels.Select(x => x.Recommend).Distinct().ToOperatorsXML() };
            return repository.Fetch<GetOperatorInfoModel>(criteria);
        }
    }
}