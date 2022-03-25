using System;
using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.ViewModels
{
    public class MailViewModel
    {
        public DateTime DateTime { get; set; }

        public string ActorUsername { get; set; }

        public string TemplatePath { get; set; }

        public string NegativeBalanceUsers { get; set; }
    }

    public class MailVoidViewModel<TViewModel> : MailViewModel
    {
        public Match Match { get; set; }

        public IEnumerable<TViewModel> Tickets { get; set; }

        public string VoidReason { get; set; }

        public string VoidReasonSC { get; set; }

        public Message Message { get; set; }

        public string ChineseSimplified => string.IsNullOrWhiteSpace(Message.ContentChineseSimplified) ? null : Message.ContentChineseSimplified.Replace("@@", "");

        public string English => string.IsNullOrWhiteSpace(Message.ContentEnglish) ? null : Message.ContentEnglish.Replace("@@", "");

        public bool IsLikelyAbnormalBet { get; set; }

        public bool IsAbnormalAccount { get; set; }
    }

    public class MailRecoverViewModel<TViewModel> : MailViewModel
    {
        public IEnumerable<Group<TViewModel>> Groups { get; set; }
    }
}