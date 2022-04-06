using Fanex.MailDeliver.Model;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface RecordVoidService<T, U>
    {
        MailRecord Create(MailVoidModel<T, U> model, EmailGroup group);
    }

    public interface IRecordRecoverService<T, U>
    {
        MailRecord Create(MailRecoverModel<T, U> model, EmailGroup group);
    }
}