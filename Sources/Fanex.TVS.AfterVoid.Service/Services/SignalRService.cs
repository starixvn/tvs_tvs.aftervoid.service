using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.SignalR;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface SignalRService
{
    Task UpdateFailedStatusForRequest(int requestId);

    Task UpdateCompletedStatusForRequests(int requestId, int? matchId, int? requestDetailId);
}

public class SignalRServiceImp(
    VoidBetAutomationService voidBetAutomationService,
    IHubContext<NotificationHub, ClientEvent> hubContext)
    : SignalRService
{
    public async Task UpdateFailedStatusForRequest(int requestId)
    {
        var failedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Failed } };
        await hubContext.Clients.All.updateRequestStatus(failedRequests);
    }

    public async Task UpdateCompletedStatusForRequests(int requestId, int? matchId, int? requestDetailId)
    {
        var requestInfo = await voidBetAutomationService.GetRequestSimpleInfo(requestId, matchId, requestDetailId);
        var completedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Completed, SimpleInfo = requestInfo } };
        await hubContext.Clients.All.updateRequestStatus(completedRequests);
    }
}