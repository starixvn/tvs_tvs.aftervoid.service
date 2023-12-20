using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Utilities;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface UpdateTicketVoidingHistoryStage
{
    Task<bool> Execute(UpdateTicketVoidingHistoryStageParameter parameter);
}

public class UpdateTicketVoidingHistoryStageImp : UpdateTicketVoidingHistoryStage
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly LoggingService loggingService;

    public UpdateTicketVoidingHistoryStageImp(VoidBetAutomationService voidBetAutomationService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
    }

    private static DateTime? GetMinWinLostDateTickets(int issueTypeId, IEnumerable<TicketEmailModel> emailTickets)
    {
        if (Helper.IsAbnormalAccount(issueTypeId) && emailTickets.Any())
        {
            return emailTickets.Min(x => x.WinLostDate);
        }

        return null;
    }

    private static DateTime? GetMaxWinLostDateTickets(int issueTypeId, IEnumerable<TicketEmailModel> emailTickets)
    {
        if (Helper.IsAbnormalAccount(issueTypeId) && emailTickets.Any())
        {
            return emailTickets.Max(x => x.WinLostDate);
        }

        return null;
    }

    public async Task<bool> Execute(UpdateTicketVoidingHistoryStageParameter parameter)
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

    private async Task<bool> TryExecute(UpdateTicketVoidingHistoryStageParameter parameter)
    {
        if (parameter.AutomationInfo.IsUpdatedVoidingHistory == true)
        {
            return true;
        }

        var transIds = Helper.IsLikelyAbnormalBet(parameter.AutomationInfo.RequestInfo.Request.IssueTypeId)
            ? string.Join(",", parameter.EmailTickets.Select(x => x.TransId)) : null;
        var minWinLostDate = GetMinWinLostDateTickets(parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, parameter.EmailTickets);
        var maxWinLostDate = GetMaxWinLostDateTickets(parameter.AutomationInfo.RequestInfo.Request.IssueTypeId, parameter.EmailTickets);
        await voidBetAutomationService.UpdateTicketVoidingHistory(parameter.AutomationInfo.VoidingHistoryId, parameter.VoidReasonId,
            parameter.NormalMessageId, parameter.SpecialMessageId, transIds, minWinLostDate, maxWinLostDate);
        await voidBetAutomationService.UpdateAfterVoidStep(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

        return true;
    }

    private async Task<bool> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
           $"ERROR: {nameof(UpdateTicketVoidingHistoryStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
           $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return false;
    }
}