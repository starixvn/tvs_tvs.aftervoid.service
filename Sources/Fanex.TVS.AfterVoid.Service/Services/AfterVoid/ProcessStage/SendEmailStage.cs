using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface SendEmailStage
{
    Task<(bool, IEnumerable<Result>)> Execute(SendEmailStageParameter parameter);
}

public class SendEmailStageImp : SendEmailStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly AfterVoidEmailService afterVoidEmailService;

    public SendEmailStageImp(VoidBetAutomationService voidBetAutomationService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService,
        AfterVoidEmailService afterVoidEmailService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
        this.afterVoidEmailService = afterVoidEmailService;
    }

    public async Task<(bool, IEnumerable<Result>)> Execute(SendEmailStageParameter parameter)
    {
        try
        {
            return await TryExecute(parameter);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, parameter.AutomationInfo);
        }
    }

    private async Task<(bool, IEnumerable<Result>)> TryExecute(SendEmailStageParameter parameter)
    {
        if (parameter.AutomationInfo.IsSentEmail == true)
        {
            return (true, new List<Result>());
        }

        var results = await afterVoidEmailService.Execute(parameter);
        await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentEmail = true });

        return (true, results);
    }

    private async Task<(bool, IEnumerable<Result>)> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(SendEmailStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return (false, new List<Result>());
    }
}