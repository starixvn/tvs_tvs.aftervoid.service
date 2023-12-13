using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Resources;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid;

public interface AfterVoidProcessStage
{
    Task CallCashOutApi(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets);

    Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults);

    Task NotifyVoidResultsSignalR(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults,
        string creatorName, IEnumerable<EmailResultModel> emailResultsHistory);

    Task WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo);
}

public class AfterVoidProcessStageImp : AfterVoidProcessStage
{
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly SendMessageService sendMessageService;
    private readonly NotificationSignalRService notificationSignalRService;
    private readonly AfterVoidEmailService afterVoidEmailService;
    private readonly LoggingService loggingService;
    private readonly CashOutHttpClientService cashOutHttpClientService;

    public AfterVoidProcessStageImp(VoidBetAutomationService voidBetAutomationService,
        SendMessageService sendMessageService,
        NotificationSignalRService notificationSignalRService,
        AfterVoidEmailService afterVoidEmailService,
        LoggingService loggingService,
        CashOutHttpClientService cashOutHttpClientService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
        this.sendMessageService = sendMessageService;
        this.notificationSignalRService = notificationSignalRService;
        this.afterVoidEmailService = afterVoidEmailService;
        this.loggingService = loggingService;
        this.cashOutHttpClientService = cashOutHttpClientService;
    }

    public async Task CallCashOutApi(VoidBetAutomationInfo automationInfo, IEnumerable<TicketEmailModel> emailTickets)
    {
        try
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
        catch (Exception exception)
        {
            await loggingService.ErrorAsync(
                $"ERROR: {nameof(CallCashOutApi)} - SessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId}",
                exception);
        }
    }

    public async Task UpdateEmailResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults)
    {
        var licenseeEmailResult = currentEmailResults.Where(x =>
            x.ErrorCode is ErrorCode.SendEmailToLicenseeSuccess or ErrorCode.SendEmailToLicenseeFail
                or ErrorCode.LicenseeSuccess or ErrorCode.LicenseeFail);
        if (licenseeEmailResult.Any())
        {
            await voidBetAutomationService.UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, licenseeEmailResult.ToVoidResultsXML());
        }
    }

    public async Task NotifyVoidResultsSignalR(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults, string creatorName, IEnumerable<EmailResultModel> emailResultsHistory)
    {
        var signalRResults = new List<Result>();
        var successfulLicensees = GetLicenseeResult(currentEmailResults, emailResultsHistory, ErrorCode.LicenseeSuccess);
        var failedLicensees = GetLicenseeResult(currentEmailResults, emailResultsHistory, ErrorCode.LicenseeFail);
        signalRResults.Insert(0, new Result { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = GetMessageVoidRequestSuccess(automationInfo.RequestId, automationInfo.MatchId) });
        if (successfulLicensees.Any())
        {
            signalRResults.Add(new Result
            {
                ErrorCode = ErrorCode.LicenseeSuccess,
                ErrorMsg = string.Format(Labels.VoidBetSendMailSuccess, string.Join(", ", successfulLicensees.Distinct()))
            });
        }
        if (failedLicensees.Any())
        {
            signalRResults.Add(new Result
            {
                ErrorCode = ErrorCode.LicenseeFail,
                ErrorMsg = string.Format(Labels.VoidBetSendMailFail, string.Join(", ", failedLicensees.Distinct()))
            });
        }

        await notificationSignalRService.NotifyVoidResultToUser(creatorName, signalRResults);
    }

    private static IEnumerable<string> GetLicenseeResult(IEnumerable<Result> currentEmailResults, IEnumerable<EmailResultModel> emailResultsHistory, int type)
    {
        var licensees = new List<string>();
        licensees.AddRange(currentEmailResults.Where(x => x.ErrorCode == type).Select(x => x.Data.ToString()));
        licensees.AddRange(emailResultsHistory.Where(x => x.Type == type).Select(x => x.Licensees));

        return licensees;
    }

    public async Task WriteLogAfterVoid(IEnumerable<Result> results, VoidBetAutomationInfo automationInfo)
    {
        var logBuilder = new StringBuilder();
        logBuilder.Append($"AfterVoidDone - BroadcastSessionId: {automationInfo.BroadcastSessionId} - VDHistoryId: {automationInfo.VoidingHistoryId} - RequestId: {automationInfo.RequestId} \n");
        foreach (var result in results)
        {
            var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
            logBuilder.Append(log);
        }

        await loggingService.InfoAsync(logBuilder.ToString());
    }

    private static string GetMessageVoidRequestSuccess(int requestId, int matchId)
    {
        return matchId > 0 ? $"Request ID: {requestId} Match ID: {matchId} voided successfully!" : $"Request ID: {requestId} voided successfully!";
    }
}