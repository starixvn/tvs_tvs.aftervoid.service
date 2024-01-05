using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage.AutoRecoverStage;

public interface SendEmailRecoverStage
{
    Task<(bool, IEnumerable<Result>)> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> tickets);
}

public class SendEmailRecoverStageImp : SendEmailRecoverStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly TVSWebSiteHttpClientService tvsWebSiteHttpClientService;

    public SendEmailRecoverStageImp(VoidBetAutomationService voidBetAutomationService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService,
        TVSWebSiteHttpClientService tvsWebSiteHttpClientService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
        this.tvsWebSiteHttpClientService = tvsWebSiteHttpClientService;
    }

    public async Task<(bool, IEnumerable<Result>)> Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> tickets)
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

    private async Task<(bool, IEnumerable<Result>)> TryExecute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> tickets)
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

    private static SendEmailRecoverApiModel CreateSendEmailRecoverApiModel(IEnumerable<TicketEmailModel> tickets, string negativeBalanceUsers)
    {
        return new SendEmailRecoverApiModel
        {
            EmailRecoverTickets = tickets,
            NegativeBalanceUsers = negativeBalanceUsers
        };
    }
}