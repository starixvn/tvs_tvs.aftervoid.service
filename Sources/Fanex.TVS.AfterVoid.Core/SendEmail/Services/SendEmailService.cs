using System;
using Fanex.MailDeliver.Model;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;
using Fanex.TVS.AfterVoid.Core.Services;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface SendEmailService
    {
        void SendEmail(MailRecord mailRecord);

        void WriteLog(EmailGroup group, Exception exception);
    }

    public class SendEmailServiceImp : SendEmailService
    {
        private readonly LoggingService loggingService;

        public SendEmailServiceImp(LoggingService loggingService)
        {
            this.loggingService = loggingService;
        }

        public void SendEmail(MailRecord mailRecord)
        {
            if (!string.IsNullOrEmpty(mailRecord.Body))
            {
                SendExistEmail(mailRecord);
            }
        }

        private static void SendExistEmail(MailRecord mailRecord)
        {
            using var mailClient = new MailDeliver.MailClient.API.MailDeliver();
            mailClient.SendMail(mailRecord);
        }

        public void WriteLog(EmailGroup group, Exception exception)
        {
            switch (group)
            {
                case EmailGroup.Csd:
                    loggingService.Error(ErrorSendEmail.Csd, exception);
                    break;

                case EmailGroup.Recover:
                    loggingService.Error(ErrorSendEmail.Recover, exception);
                    break;

                case EmailGroup.CashOut:
                    loggingService.Error(ErrorSendEmail.CashOut, exception);
                    break;

                case EmailGroup.ItQa:
                case EmailGroup.ItSupport:
                    loggingService.Error(ErrorSendEmail.It, exception);
                    break;

                default:
                    loggingService.Error(ErrorSendEmail.WrongGroup, exception);
                    break;
            }
        }
    }
}