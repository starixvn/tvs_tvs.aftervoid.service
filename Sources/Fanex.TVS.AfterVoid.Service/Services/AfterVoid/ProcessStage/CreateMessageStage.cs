using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CreateMessageStage
{
    Task<(int, int)> Execute(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes,
        IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets);
}

public class CreateMessageStageImp : CreateMessageStage
{
    private readonly LoggingService loggingService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly SendLetstalkServiceStage sendLetstalkServiceStage;
    private readonly SendMessageService sendMessageService;
    private readonly SpecialPlatformService specialPlatformService;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public CreateMessageStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        LoggingService loggingService,
        SendLetstalkServiceStage sendLetstalkServiceStage,
        SpecialPlatformService specialPlatformService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.loggingService = loggingService;
        this.sendLetstalkServiceStage = sendLetstalkServiceStage;
        this.specialPlatformService = specialPlatformService;
    }

    public async Task<(int, int)> Execute(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes,
        IEnumerable<TicketEmailModel> normalTickets, IEnumerable<TicketEmailModel> specialTickets)
    {
        try
        {
            var normalMessageId = await CreateMessageForNormalPlatform(automationInfo, matchInfo, allVoidDataInCurrentVoidTimes, normalTickets);
            var specialMessageId = await CreateMessageForSpecialPlatform(automationInfo, matchInfo, allVoidDataInCurrentVoidTimes, specialTickets);

            return (normalMessageId, specialMessageId);
        }
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId}", exception);
            await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
            await sendLetstalkServiceStage.InsertLetstalk(automationInfo.RequestId, automationInfo.BroadcastSessionId, ExecuteStatus.Failed);
            await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);

            return (0, 0);
        }
    }

    private async Task<int> CreateMessageForNormalPlatform(VoidBetAutomationInfo automationInfo, Match matchInfo,
        IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes, IEnumerable<TicketEmailModel> voidedTickets)
    {
        if (!voidedTickets.Any())
        {
            return 0;
        }

        Message message;
        if (automationInfo.MessageId != null && automationInfo.MessageId != 0)
        {
            message = await sendMessageService.GetMessage(automationInfo.MessageId.Value);
        }
        else
        {
            message = await InitializeMessageContent(automationInfo, matchInfo, allVoidDataInCurrentVoidTimes);
            message.MessageId = await sendMessageService.InsertMessage(message, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);

            await voidBetAutomationService.UpdateMessageIdByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, message.MessageId, null);
        }

        return message.MessageId;
    }

    private async Task<int> CreateMessageForSpecialPlatform(VoidBetAutomationInfo automationInfo, Match matchInfo,
        IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes, IEnumerable<TicketEmailModel> voidedTickets)
    {
        if (!voidedTickets.Any())
        {
            return 0;
        }

        Message message;
        if (automationInfo.MessageIdSpecial != null && automationInfo.MessageIdSpecial != 0)
        {
            message = await sendMessageService.GetMessage(automationInfo.MessageIdSpecial.Value);
        }
        else
        {
            message = await InitializeMessageContent(automationInfo, matchInfo, allVoidDataInCurrentVoidTimes);
            message = specialPlatformService.RemoveSabaInMessageContent(matchInfo, message);
            message.MessageId = await sendMessageService.InsertMessage(message, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);

            await voidBetAutomationService.UpdateMessageIdByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, null, message.MessageId);
        }

        return message.MessageId;
    }

    private async Task<Message> InitializeMessageContent(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoidTimes)
    {
        if (automationInfo.IsAutoVoid)
        {
            return await sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, allVoidDataInCurrentVoidTimes, automationInfo.RequestInfo);
        }

        return sendMessageService.InitializeMessageContentForManualVoid(allVoidDataInCurrentVoidTimes.First());
    }
}