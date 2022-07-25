using Fanex.TVS.AfterVoid.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TicketVoidingSystem.Core.SignalR
{
    public class NotificationHub : Hub<ClientEvent>
    {
    }

    public interface ClientEvent
    {
        Task showVoidResult(IEnumerable<Result> results);

        Task updateRequestStatus(IEnumerable<RequestStatus> status);

        Task showErrorMessage(string message);
    }
}