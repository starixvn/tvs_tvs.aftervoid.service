using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Microsoft.AspNetCore.SignalR;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Resources;
using TicketVoidingSystem.SignalR;

namespace Fanex.TVS.AfterVoid.Service.Services;

public interface NotificationSignalRService
{
    Task UpdateFailedStatusForRequest(int requestId);

    Task UpdateCompletedStatusForRequests(int requestId, int matchId);

    Task NotifyVoidResultToUser(string userName, IEnumerable<Result> results);

    Task NotifyErrorWhenExecuteAfterVoidServiceToUser(string userName);
}

public class NotificationSignalRServiceImp : NotificationSignalRService
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly IHubContext<NotificationHub, ClientEvent> hubContext;

    public NotificationSignalRServiceImp(VoidBetAutomationService voidBetAutomationService, IHubContext<NotificationHub, ClientEvent> hubContext)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.hubContext = hubContext;
    }

    public async Task UpdateFailedStatusForRequest(int requestId)
    {
        var failedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Failed } };
        await hubContext.Clients.All.updateRequestStatus(failedRequests);
    }

    public async Task UpdateCompletedStatusForRequests(int requestId, int matchId)
    {
        var requestInfo = await voidBetAutomationService.GetRequestSimpleInfo(requestId, matchId);
        var completedRequests = new List<RequestStatus> { new() { RequestId = requestId, Status = RequestStatusConstant.Completed, SimpleInfo = requestInfo } };
        await hubContext.Clients.All.updateRequestStatus(completedRequests);
    }

    public async Task NotifyVoidResultToUser(string userName, IEnumerable<Result> results)
    {
        await hubContext.Clients.User(userName).showVoidResult(results);
    }

    public async Task NotifyErrorWhenExecuteAfterVoidServiceToUser(string userName)
    {
        var results = new List<Result>
        {
            new()
            {
                ErrorCode = ErrorCode.ExceptionWhenRunAfterVoid,
                ErrorMsg = Messages.ExceptionWhenRunAfterVoid
            }
        };

        await NotifyVoidResultToUser(userName, results);
    }
}