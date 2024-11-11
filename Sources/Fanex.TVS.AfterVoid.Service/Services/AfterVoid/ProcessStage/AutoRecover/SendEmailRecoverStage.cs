using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecover;

public interface SendEmailRecoverStage
{
    Task<(bool, IEnumerable<Result>)> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets);
}

public class SendEmailRecoverStageImp(
    VoidBetAutomationService voidBetAutomationService,
    NotificationSignalRService notificationSignalRService,
    LoggingService loggingService,
    TVSWebSiteHttpClientService tvsWebSiteHttpClientService)
    : SendEmailRecoverStage
{
    public async Task<(bool, IEnumerable<Result>)> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets)
    {
        try
        {
            return await TryExecute(automationInfo, tickets);
        }
        catch (Exception exception)
        {
            return await CatchException(exception, automationInfo);
        }
    }

    private async Task<(bool, IEnumerable<Result>)> TryExecute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketRecoverEmailModel> tickets)
    {
        if (automationInfo.IsSentEmail == true)
        {
            return (true, new List<Result>());
        }

        var sendEmailRecoverApiModel = CreateSendEmailRecoverApiModel(tickets, automationInfo.NegativeBalanceUsers);

        var (isSentEmail, results) = await tvsWebSiteHttpClientService.SendEmailRecover(sendEmailRecoverApiModel);

        if (!isSentEmail)
        {
            throw new ExternalException($"{nameof(SendEmailRecoverStage)}.{nameof(Execute)}");
        }

        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsSentEmail = true });
        return (true, results);
    }

    private async Task<(bool, IEnumerable<Result>)> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(SendEmailRecoverStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId} - " +
            $"VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

        return (false, new List<Result>());
    }

    private static SendEmailRecoverApiModel CreateSendEmailRecoverApiModel(IEnumerable<TicketRecoverEmailModel> tickets, string negativeBalanceUsers)
    {
        return new SendEmailRecoverApiModel
        {
            Tickets = tickets,
            NegativeBalanceUsers = negativeBalanceUsers,
            ActorUsername = AutoVoidConstant.ActorName
        };
    }
}