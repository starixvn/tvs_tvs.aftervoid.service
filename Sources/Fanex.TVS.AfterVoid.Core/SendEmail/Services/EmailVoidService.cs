using System;
using System.Collections.Generic;
using System.Linq;
using Fanex.TVS.AfterVoid.Core.Constants;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.Resources;
using Fanex.TVS.AfterVoid.Core.SendEmail.Constants;
using Fanex.TVS.AfterVoid.Core.SendEmail.Extensions;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels;
using Force.DeepCloner;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface EmailVoidService<TViewModel, TModel>
    {
        List<SiteEmail> GetSites();

        List<SiteEmail> GetSitesByCategory(bool isAbnormalBet);

        SiteEmail GetSiteByName(List<SiteEmail> sites, string name);

        MailVoidModel<TViewModel, TModel> CreateMailModel(MailVoidViewModel<TViewModel> viewModel, SiteEmail site, List<TModel> tickets);

        MailLicenseesModel<TViewModel, TModel> CreateMailModel(MailVoidViewModel<TViewModel> viewModel, List<SiteEmail> sites, List<TModel> tickets);

        List<Result> SendEmail(EmailVoidModel model, List<TModel> tickets);

        void SendToCashOut(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets);

        void SendToItQa(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets);

        void SendToItSupport(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets);

        void Send(MailVoidModel<TViewModel, TModel> model, EmailGroup group);
    }

    public class EmailVoidServiceImp<TViewModel, TModel> : EmailVoidService<TViewModel, TModel> where TModel : EmailSupport
    {
        private readonly RecordVoidService<TViewModel, TModel> recordService;
        private readonly SendEmailService sendEmailService;
        private readonly MailMessageService mailMessageService;
        private readonly SitesEmailService sitesService;

        public EmailVoidServiceImp(
            RecordVoidService<TViewModel, TModel> recordService,
            SendEmailService sendEmailService,
            MailMessageService mailMessageService,
            SitesEmailService sitesService)
        {
            this.recordService = recordService;
            this.sendEmailService = sendEmailService;
            this.mailMessageService = mailMessageService;
            this.sitesService = sitesService;
        }

        public List<SiteEmail> GetSites()
        {
            return sitesService.GetSites();
        }

        public List<SiteEmail> GetSitesByCategory(bool isAbnormalBet)
        {
            return sitesService.GetSitesByCategory(isAbnormalBet);
        }

        public SiteEmail GetSiteByName(List<SiteEmail> sites, string name)
        {
            return sitesService.GetSiteByName(sites, name);
        }

        public MailVoidModel<TViewModel, TModel> CreateMailModel(MailVoidViewModel<TViewModel> viewModel, SiteEmail site, List<TModel> tickets)
        {
            return new MailVoidModel<TViewModel, TModel> { ViewModel = viewModel, Site = site, Tickets = tickets };
        }

        public MailLicenseesModel<TViewModel, TModel> CreateMailModel(MailVoidViewModel<TViewModel> viewModel, List<SiteEmail> sites, List<TModel> tickets)
        {
            return new MailLicenseesModel<TViewModel, TModel> { ViewModel = viewModel, Sites = sites, Tickets = tickets };
        }

        public List<Result> SendEmail(EmailVoidModel model, List<TModel> tickets)
        {
            var viewModel = CreateMailViewModel(model);
            var sites = GetSitesByCategory(model.IsLikelyAbnormalBet);
            var mailModel = CreateMailModel(viewModel, sites, tickets);

            var results = ToLicensees(mailModel);

            SendToCashOut(viewModel, sites, tickets);

            viewModel.NegativeBalanceUsers = model.NegativeBalanceUsers;
            SendToItQa(viewModel, sites, tickets);

            viewModel.NegativeBalanceUsers = model.NegativeBalanceUsers;
            SendToItSupport(viewModel, sites, tickets);

            return results;
        }

        private MailVoidViewModel<TViewModel> CreateMailViewModel(EmailVoidModel model)
        {
            return new MailVoidViewModel<TViewModel>
            {
                TemplatePath = TemplateName.VoidBetChinese,
                Match = model.Match,
                DateTime = DateTime.Now,
                VoidReason = model.Reason,
                VoidReasonSC = model.ReasonSC,
                Message = model.Message,
                NegativeBalanceUsers = model.NegativeBalanceUsers,
                IsLikelyAbnormalBet = model.IsLikelyAbnormalBet,
                IsAbnormalAccount = model.IsAbnormalAccount
            };
        }

        public List<Result> ToLicensees(MailLicenseesModel<TViewModel, TModel> mailLicenseesModel)
        {
            var model = mailLicenseesModel.DeepClone();
            var results = new CombineMessageModel();
            var negativeBalanceUsers = model.ViewModel.NegativeBalanceUsers;
            var tickets = model.Tickets;
            foreach (var site in model.Sites)
            {
                var siteName = site.Site;
                var ticketsInSite = tickets.Where(t => t.EmailGroup.Equals(siteName, StringComparison.CurrentCultureIgnoreCase)).ToList();
                var mailModel = CreateMailModel(model.ViewModel, site, ticketsInSite);
                if (mailModel.ViewModel.Match != null && mailModel.ViewModel.Match.LeagueGroupId.IsLeagueGroupIdSaBa() && site.IsChinaView)
                {
                    ReplaceMessageChinaViewLicensee(mailModel);
                }
                if (mailModel.Tickets.Count > 0)
                {
                    mailModel.ViewModel.TemplatePath = TemplateName.VoidBetChinese;
                    mailModel.ViewModel.NegativeBalanceUsers = negativeBalanceUsers;
                    ToLicensee(mailModel, results);
                }
            }

            return CreateCombineResults(results);
        }

        private static void ReplaceMessageChinaViewLicensee(MailVoidModel<TViewModel, TModel> mailModel)
        {
            var serializedContentChineseSimplified = JsonConvert.SerializeObject(mailModel.ViewModel.Message.ContentChineseSimplified);
            var deSerializedContentChineseSimplified = JsonConvert.DeserializeObject<string>(serializedContentChineseSimplified.Replace("沙巴", ""));

            mailModel.ViewModel.Message.ContentChineseSimplified = deSerializedContentChineseSimplified;
            mailModel.ViewModel.Message.ContentEnglish = mailModel.ViewModel.Message.ContentEnglish.Replace("SABA ", "");
        }

        private void ToLicensee(MailVoidModel<TViewModel, TModel> model, CombineMessageModel results)
        {
            var site = model.Site;
            if (!string.IsNullOrEmpty(site.ToList))
            {
                SendEmailToSite(model, results);
            }
            else
            {
                results.Results.Add(mailMessageService.CreateFail(site.Site));
                results.FailedLicensees.Add(site.Site);
            }
        }

        private void SendEmailToSite(MailVoidModel<TViewModel, TModel> model, CombineMessageModel results)
        {
            var site = model.Site;
            try
            {
                var mailRecord = recordService.Create(model, EmailGroup.Csd);
                sendEmailService.SendEmail(mailRecord);
                var listEmail = site.ToList.Split(';').Distinct().ToArray();
                results.Results.Add(mailMessageService.CreateSuccess(site.Site, listEmail));
                results.SuccessfulLicensees.Add(site.Site);
            }
            catch (Exception exception)
            {
                sendEmailService.WriteLog(EmailGroup.Csd, exception);
                results.Results.Add(mailMessageService.CreateFail(site.Site));
                results.FailedLicensees.Add(site.Site);
            }
        }

        private List<Result> CreateCombineResults(CombineMessageModel model)
        {
            var results = model.Results;
            var successfulLicensees = string.Join(", ", model.SuccessfulLicensees);
            var failedLicensees = string.Join(", ", model.FailedLicensees);
            return mailMessageService.InsertCombineLicenseesName(results, successfulLicensees, failedLicensees);
        }

        public void SendToCashOut(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets)
        {
            var viewModel = mailVoidViewModel.DeepClone();
            var model = new MailVoidModel<TViewModel, TModel>
            {
                ViewModel = viewModel,
                Site = GetSiteByName(sites, Labels.CashOut),
                Tickets = tickets.Where(s => s.IsCashOut).ToList()
            };

            Send(model, EmailGroup.CashOut);
        }

        public void SendToItQa(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets)
        {
            var viewModel = mailVoidViewModel.DeepClone();
            var model = new MailVoidModel<TViewModel, TModel>
            {
                ViewModel = viewModel,
                Site = GetSiteByName(sites, Labels.ItQa),
                Tickets = tickets.Where(s => s.Currency == CurrencyConstant.UusId).ToList()
            };

            Send(model, EmailGroup.ItQa);
        }

        public void SendToItSupport(MailVoidViewModel<TViewModel> mailVoidViewModel, List<SiteEmail> sites, List<TModel> tickets)
        {
            var viewModel = mailVoidViewModel.DeepClone();
            var model = new MailVoidModel<TViewModel, TModel>
            {
                ViewModel = viewModel,
                Site = GetSiteByName(sites, Labels.ItSupport),
                Tickets = tickets.Where(s => s.IsITSupportCreatedRequest && s.IsItSupport && s.Currency != CurrencyConstant.UusId).ToList()
            };

            Send(model, EmailGroup.ItSupport);
        }

        public void Send(MailVoidModel<TViewModel, TModel> model, EmailGroup group)
        {
            try
            {
                var mailRecord = recordService.Create(model, group);
                sendEmailService.SendEmail(mailRecord);
            }
            catch (Exception exception)
            {
                sendEmailService.WriteLog(group, exception);
            }
        }
    }
}