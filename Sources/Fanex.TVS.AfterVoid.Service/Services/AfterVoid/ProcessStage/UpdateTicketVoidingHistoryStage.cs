using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface UpdateTicketVoidingHistoryStage
{
    Task<bool> Execute(VoidBetAutomationInfo automationInfo, int normalMessageId, int specialMessageId, int reasonId, IEnumerable<TicketEmailModel> emailTickets);
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

    public async Task<bool> Execute(VoidBetAutomationInfo automationInfo, int normalMessageId, int specialMessageId, int reasonId, IEnumerable<TicketEmailModel> emailTickets)
    {
        try
        {
            if (automationInfo.IsUpdatedVoidingHistory == true)
            {
                return true;
            }

            var transIds = Helper.IsLikelyAbnormalBet(automationInfo.RequestInfo.Request.IssueTypeId)
                ? string.Join(",", emailTickets.Select(x => x.TransId)) : null;
            var minWinLostDate = GetMinWinLostDateTickets(automationInfo.RequestInfo.Request.IssueTypeId, emailTickets);
            var maxWinLostDate = GetMaxWinLostDateTickets(automationInfo.RequestInfo.Request.IssueTypeId, emailTickets);
            await voidBetAutomationService.UpdateTicketVoidingHistory(automationInfo.VoidingHistoryId, reasonId,
                normalMessageId, specialMessageId, transIds, minWinLostDate, maxWinLostDate);

            await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsUpdatedVoidingHistory = true });

            return true;
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
                $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return false;
        }
    }
}