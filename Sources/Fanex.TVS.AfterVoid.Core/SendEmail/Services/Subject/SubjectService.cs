namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services.Subject
{
    using Extensions;
    using Models;
    using Resources;

    public interface SubjectService
    {
        string CreateVoid<T, U>(MailVoidModel<T, U> model, EmailGroup group, VoidType type) where U : EmailSupport;

        string CreateRecover<T, U>(MailRecoverModel<T, U> model, EmailGroup group, RecoverType type);
    }

    public class SubjectServiceImp : SubjectService
    {
        public string CreateVoid<T, U>(MailVoidModel<T, U> model, EmailGroup group, VoidType type) where U : EmailSupport
        {
            string subject;
            var labelService = SubjectFactory.CreateVoid(type);
            switch (group)
            {
                case EmailGroup.Csd:
                    subject = SubjectLicensee(model, labelService.Licensee());
                    break;

                case EmailGroup.CashOut:
                    subject = SubjectCashOut(model, labelService.Licensee());
                    break;

                case EmailGroup.ItQa:
                    subject = SubjectItQa(model, labelService.It());
                    break;

                case EmailGroup.ItSupport:
                    subject = SubjectItSupport(model, labelService.It());
                    break;

                default:
                    subject = ExceptionGroup.WrongGroup;
                    break;
            }

            return subject;
        }

        private static string SubjectLicensee<T, U>(MailVoidModel<T, U> model, string format)
        {
            var siteName = model.Site.Site;
            var dateTime = model.ViewModel.DateTime;
            return string.Format(format, siteName.ToUpperInvariant(), dateTime);
        }

        private static string SubjectCashOut<T, U>(MailVoidModel<T, U> model, string format)
        {
            var siteName = Labels.CashOut;
            var dateTime = model.ViewModel.DateTime;
            return string.Format(format, siteName, dateTime);
        }

        private static string SubjectItQa<T, U>(MailVoidModel<T, U> model, string format) where U : EmailSupport
        {
            return SubjectItGroup(model, Labels.TestCurrency, format);
        }

        private static string SubjectItSupport<T, U>(MailVoidModel<T, U> model, string format) where U : EmailSupport
        {
            return SubjectItGroup(model, Labels.RealCurrency, format);
        }

        private static string SubjectItGroup<T, U>(MailVoidModel<T, U> model, string prefix, string format) where U : EmailSupport
        {
            var sites = model.Tickets.JoinSites(s => s.Site);
            var dateTime = model.ViewModel.DateTime;
            return string.Format(format, prefix, sites.ToUpperInvariant(), dateTime);
        }

        public string CreateRecover<T, U>(MailRecoverModel<T, U> model, EmailGroup group, RecoverType type)
        {
            var labels = SubjectFactory.CreateRecover(type);
            switch (group)
            {
                case EmailGroup.Recover:
                case EmailGroup.CashOut:
                    return string.Format(labels.Licensee(), model.ViewModel.DateTime);

                case EmailGroup.ItQa:
                    return string.Format(labels.It(), Labels.TestCurrency, model.ViewModel.DateTime);

                case EmailGroup.ItSupport:
                    return string.Format(labels.It(), Labels.RealCurrency, model.ViewModel.DateTime);

                default:
                    return ExceptionGroup.WrongGroup;
            }
        }
    }
}