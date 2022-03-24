//namespace Fanex.TVS.AfterVoid.Core.SendEmail.Models
//{
//    using System.Collections.Generic;
//    using DTO;
//    using ViewModels;

//    public class MailModel<TModel>
//    {
//        public SiteEmail Site { get; set; }

//        public List<TModel> Tickets { get; set; }
//    }

//    public class MailVoidModel<TViewModel, TModel> : MailModel<TModel>
//    {
//        public MailVoidViewModel<TViewModel> ViewModel { get; set; }
//    }

//    public class MailRecoverModel<TViewModel, TModel> : MailModel<TModel>
//    {
//        public MailRecoverViewModel<TViewModel> ViewModel { get; set; }
//    }

//    public class MailLicenseesModel<TViewModel, TModel>
//    {
//        public MailVoidViewModel<TViewModel> ViewModel { get; set; }

//        public List<SiteEmail> Sites { get; set; }

//        public List<TModel> Tickets { get; set; }
//    }
//}