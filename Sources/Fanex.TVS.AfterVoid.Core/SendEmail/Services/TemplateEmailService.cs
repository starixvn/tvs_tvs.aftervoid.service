using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using RazorEngine;
using RazorEngine.Templating;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface TemplateEmailService
    {
        MailRecordResult Create<T, U>(MailVoidModel<T, U> model, string subject);

        MailRecordResult Create<T, U>(MailRecoverModel<T, U> model, string subject);

        string GetEmailBody<T>(string path, T model);

        List<Group<BetTrans>> GenerateRecoverGroups(List<BetTrans> listTicket);

        string BetTeamAddExcludingScore(string betTeam, string score);
    }

    public class TemplateEmailServiceImp : TemplateEmailService
    {
        public MailRecordResult Create<T, U>(MailVoidModel<T, U> model, string subject)
        {
            return new MailRecordResult
            {
                SiteEmail = model.Site,
                Subject = subject,
                EmailBody = GetEmailBody(model.ViewModel.TemplatePath, model.ViewModel)
            };
        }

        public MailRecordResult Create<T, U>(MailRecoverModel<T, U> model, string subject)
        {
            return new MailRecordResult
            {
                SiteEmail = model.Site,
                Subject = subject,
                EmailBody = GetEmailBody(model.ViewModel.TemplatePath, model.ViewModel)
            };
        }

        public virtual string GetEmailBody<T>(string path, T model)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            var template = File.ReadAllText(fullPath);
            var key = typeof(T) + path;
            return Engine.Razor.RunCompile(template, key, typeof(T), model);
        }

        public List<Group<BetTrans>> GenerateRecoverGroups(List<BetTrans> listTicket)
        {
            var groups = listTicket.GroupBy(ticket => ticket.Site);
            return groups.Select(group => new Group<BetTrans>
            {
                Name = group.Key.ToUpperInvariant(),
                Tickets = group.Select(ticket => ticket).ToList()
            }).ToList();
        }

        public string BetTeamAddExcludingScore(string betTeam, string score)
        {
            return betTeam.Contains("AOS") ? betTeam + score : betTeam;
        }
    }
}