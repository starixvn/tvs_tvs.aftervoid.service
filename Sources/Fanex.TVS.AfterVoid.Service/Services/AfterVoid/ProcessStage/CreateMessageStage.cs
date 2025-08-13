using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CreateMessageStage
{
    Task<(int, int, Message, Message)> Execute(CreateMessageStageParameter parameter);
}

public class CreateMessageStageImp(
    VoidBetAutomationService voidBetAutomationService,
    SendMessageService sendMessageService,
    SignalRService signalRService,
    LoggingService loggingService,
    SpecialPlatformService specialPlatformService,
    MessageContentParameterService messageContentParameterService)
    : CreateMessageStage
{
    public async Task<(int, int, Message, Message)> Execute(CreateMessageStageParameter parameter)
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

    public async Task<(int, int, Message, Message)> TryExecute(CreateMessageStageParameter parameter)
    {
        parameter.AutomationInfo.RequestInfo.IsAllLiveTickets
            = messageContentParameterService.IsAllLiveTickets(parameter.NormalTickets, parameter.SpecialTickets);
        parameter.AutomationInfo.RequestInfo.SpecialBetTypeContent
            = await messageContentParameterService.GetSpecialBetTypeContent(parameter.AutomationInfo, parameter.NormalTickets, parameter.SpecialTickets);

        var normalMessageContent = await InitializeMessageContent(parameter.AutomationInfo, parameter.MatchInfo, parameter.VoidDataXml);
        var specialMessageContent = specialPlatformService.RemoveSabaInMessageContent(parameter.MatchInfo, normalMessageContent, parameter.AutomationInfo.RequestInfo);
        var normalMessageId = await CreateMessageForNormalPlatform(parameter.AutomationInfo, parameter.MatchInfo, parameter.NormalTickets, normalMessageContent);
        var specialMessageId = await CreateMessageForSpecialPlatform(parameter.AutomationInfo, parameter.MatchInfo, parameter.SpecialTickets, specialMessageContent);

        return (normalMessageId, specialMessageId, normalMessageContent, specialMessageContent);
    }

    private async Task<(int, int, Message, Message)> CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync(
            $"ERROR: {nameof(CreateMessageStage)}.{nameof(Execute)} - BroadcastSessionId: {automationInfo.BroadcastSessionId}", exception);
        await voidBetAutomationService.UpdateServiceStatusByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, ExecuteStatus.Failed);
        await signalRService.UpdateFailedStatusForRequest(automationInfo.RequestId);
        return (0, 0, null, null);
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

    private async Task<int> CreateMessageForSpecialPlatform(VoidBetAutomationInfo automationInfo, Match matchInfo, IEnumerable<TicketEmailModel> voidedTickets, Message specialMessageContent)
    {
        if (!voidedTickets.Any())
        {
            return 0;
        }

        if (automationInfo.MessageIdSpecial != null && automationInfo.MessageIdSpecial != 0)
        {
            return automationInfo.MessageIdSpecial.Value;
        }

        specialMessageContent.MessageId = await sendMessageService.InsertMessage(
            specialMessageContent, matchInfo?.MatchId ?? 0, matchInfo?.LeagueGroupId, automationInfo.RequestInfo.Request.IssueTypeId);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { MessageIdSpecial = specialMessageContent.MessageId });

        return specialMessageContent.MessageId;
    }

    private async Task<Message> InitializeMessageContent(VoidBetAutomationInfo automationInfo, Match matchInfo, VoidDataXmlModel voidDataXml)
    {
        if (automationInfo.IsAutoVoid)
        {
            return await sendMessageService.InitializeMessageContentForAutoVoid(matchInfo, voidDataXml, automationInfo.RequestInfo);
        }

        return sendMessageService.InitializeMessageContentForManualVoid(voidDataXml);
    }
}