using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CallCashOutApiStage
{
    Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets);
}

public class CallCashOutApiStageImp : CallCashOutApiStage
{
    private readonly LoggingService loggingService;
    private readonly CashOutHttpClientService cashOutHttpClientService;
    private readonly VoidBetAutomationService voidBetAutomationService;

    public CallCashOutApiStageImp(VoidBetAutomationService voidBetAutomationService,
        CashOutHttpClientService cashOutHttpClientService,
        LoggingService loggingService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.cashOutHttpClientService = cashOutHttpClientService;
        this.loggingService = loggingService;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets)
    {
        try
        {
            await TryExecute(automationInfo, emailTickets);
        }
        catch (Exception exception)
        {
            await CatchException(exception, automationInfo);
        }
    }

    private async Task TryExecute(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets)
    {
        if (automationInfo.IsRunExternalService == true)
        {
            return;
        }
        var basketballCashOutTickets = emailTickets.Where(x => x.IsCashOut && x.SportType == SportTypeConstant.Basketball);
        if (!basketballCashOutTickets.Any())
        {
            return;
        }
        var requestModel = new CashOutApiModel { VoidBetInfos = basketballCashOutTickets.Select(x => new VoidBetInfo { TransId = x.TransId.ToString(), CustId = x.CustId.ToString() }) };
        await cashOutHttpClientService.VoidBet(requestModel);
        await voidBetAutomationService.UpdateAfterVoidStep(automationInfo.RequestId, automationInfo.VoidingHistoryId, new VoidBetAutomationInfo { IsRunExternalService = true });
    }

    private async Task CatchException(Exception exception, VoidBetAutomationInfo automationInfo)
    {
        await loggingService.ErrorAsync($"ERROR: {nameof(CallCashOutApiStage)}.{nameof(Execute)} - SessionId: {automationInfo.BroadcastSessionId}" +
                                        $" - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}", exception);
    }
}