using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using Fanex.TVS.AfterVoid.Service.Services.Letstalk;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CreateMessageStage
{
    Task<(int, int, Message)> Execute(CreateMessageStageParameter parameter);
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

    public async Task<(int, int, Message)> Execute(CreateMessageStageParameter parameter)
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

    public async Task<(int, int, Message)> TryExecute(CreateMessageStageParameter parameter)
    {
        var normalMessageContent = await InitializeMessageContent(parameter.AutomationInfo, parameter.MatchInfo, parameter.AllVoidDataInCurrentVoid);
        var normalMessageId = await CreateMessageForNormalPlatform(parameter.AutomationInfo, parameter.MatchInfo, parameter.NormalTickets, normalMessageContent);
        var specialMessageId = await CreateMessageForSpecialPlatform(parameter.AutomationInfo, parameter.MatchInfo, parameter.SpecialTickets, normalMessageContent);

        return (normalMessageId, specialMessageId, normalMessageContent);
    }

    private async Task<(int, int, Message)> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(CreateMessageStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await sendLetstalkServiceStage.InsertLetstalk(automationInfo.RequestId, automationInfo.BroadcastSessionId, ExecuteStatus.Failed);
        await notificationSignalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);
        return (0, 0, null);
    }

    private async Task<int> CreateMessageForNormalPlatform(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<TicketEmailModel> voidedTickets, Message initMessage)
    {
        if (!voidedTickets.Any())
        {
            return 0;
        }

        if (automationInfo.MessageId != null && automationInfo.MessageId != 0)
        {
            return automationInfo.MessageId.Value;
        }

        var normalMessageId = await sendMessageService.InsertMessage(initMessage, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { MessageId = normalMessageId });

        return normalMessageId;
    }

    private async Task<int> CreateMessageForSpecialPlatform(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<TicketEmailModel> voidedTickets, Message initMessage)
    {
        if (!voidedTickets.Any())
        {
            return 0;
        }

        if (automationInfo.MessageIdSpecial != null && automationInfo.MessageIdSpecial != 0)
        {
            return automationInfo.MessageIdSpecial.Value;
        }

        var messageSpecial = specialPlatformService.RemoveSabaInMessageContent(matchInfo, initMessage);
        messageSpecial.MessageId = await sendMessageService.InsertMessage(messageSpecial, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { MessageIdSpecial = messageSpecial.MessageId });

        return messageSpecial.MessageId;
    }

    private async Task<Message> InitializeMessageContent(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<VoidDataXmlModel> allVoidDataInCurrentVoid)
    {
        if (automationInfo.IsAutoVoid)
        {
            return await sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, allVoidDataInCurrentVoid, automationInfo.RequestInfo);
        }

        return sendMessageService.InitializeMessageContentForManualVoid(allVoidDataInCurrentVoid.First());
    }
}