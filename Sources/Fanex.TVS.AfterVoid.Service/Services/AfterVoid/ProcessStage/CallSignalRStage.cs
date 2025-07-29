using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.Notification;
using TicketVoidingSystem.Constants;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CallSignalRStage
{
    Task ExecuteForAutoVoid(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo);

    Task ExecuteForAutoRecover(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo);

    Task ExecuteForManualVoid(CallSignalRManualVoidParameter parameter);
}

public class CallSignalRStageImp(SignalRService signalRService, NotificationService notificationService) : CallSignalRStage
{
    public async Task ExecuteForAutoVoid(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await signalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId, automationInfo.RequestDetailId);
            var content = MessageHelper.CreateVoidSuccessfully(automationInfo.RequestId);
            notificationService.ExecuteBackground(() => notificationService.InsertNotification(NotificationType.Notification, content, automationInfo.RequestId));
        }
    }

    public async Task ExecuteForAutoRecover(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await signalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId, automationInfo.RequestDetailId);
            var content = MessageHelper.CreateRecoverSuccessfully(automationInfo.RequestId, automationInfo.RequestInfo.Request.IsHasResettledTicket);
            notificationService.ExecuteBackground(() => notificationService.InsertNotification(NotificationType.Notification, content, automationInfo.RequestId));
        }
    }

    public async Task ExecuteForManualVoid(CallSignalRManualVoidParameter parameter)
    {
        if (parameter.IsLastedInRequest && parameter.CurrentRequestStatus != RequestStatusConstant.Failed)
        {
            await signalRService.UpdateCompletedStatusForRequests(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.MatchId, parameter.AutomationInfo.RequestDetailId);
            var content = MessageHelper.CreateVoidSuccessfully(parameter.AutomationInfo.RequestId);
            notificationService.ExecuteBackground(() => notificationService.InsertNotification(NotificationType.Notification, content, parameter.AutomationInfo.RequestId));
        }
    }
}