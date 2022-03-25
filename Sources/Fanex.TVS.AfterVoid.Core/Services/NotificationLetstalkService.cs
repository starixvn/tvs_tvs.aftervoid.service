using System;
using System.Collections.Generic;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface NotificationLetstalkService
    {
        bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets);
    }

    public class NotificationLetstalkServiceImp : NotificationLetstalkService
    {
        public bool Send(NotificationLetstalkModel model, IEnumerable<TicketEmailModel> tickets)
        {
            throw new NotImplementedException();
        }
    }
}