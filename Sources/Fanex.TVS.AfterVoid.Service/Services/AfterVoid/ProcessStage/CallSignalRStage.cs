using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using Fanex.TVS.AfterVoid.Service.Models.Parameters;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Resources;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface CallSignalRStage
{
    Task ExecuteForAutoVoid(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo);

    Task ExecuteForAutoRecover(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo);

    Task ExecuteForManualVoid(CallSignalRManualVoidParameter parameter);
}

public class CallSignalRStageImp(NotificationSignalRService notificationSignalRService) : CallSignalRStage
{
    public async Task ExecuteForAutoVoid(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);
        }
    }

    public async Task ExecuteForAutoRecover(bool isLastedInRequest, string currentRequestStatus, VoidBetAutomationInfo automationInfo)
    {
        if (isLastedInRequest && currentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(automationInfo.RequestId, automationInfo.MatchId);

            var messageSuccessful = $"Request ID: {automationInfo.RequestId} recovered successfully!";
            if (automationInfo.RequestInfo.Request.IsHasResettledTicket)
            {
                messageSuccessful += $"{Environment.NewLine}Please be aware that some tickets have been re-settled automatically.";
            }
            var signalRResults = new List<Result> { new() { ErrorCode = ErrorCode.VoidRequestSuccess, ErrorMsg = messageSuccessful } };
            await notificationSignalRService.NotifyVoidResultToUser(automationInfo.RequestInfo.Request.RequestedBy, signalRResults);
        }
    }

    public async Task ExecuteForManualVoid(CallSignalRManualVoidParameter parameter)
    {
        if (parameter.IsLastedInRequest && parameter.CurrentRequestStatus != RequestStatusConstant.Failed)
        {
            await notificationSignalRService.UpdateCompletedStatusForRequests(parameter.AutomationInfo.RequestId, parameter.AutomationInfo.MatchId);
            await NotifyVoidResult(parameter.AutomationInfo, parameter.CurrentEmailResults, parameter.CreatorName, parameter.EmailResultsHistory);
        }
    }

    private async Task NotifyVoidResult(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults, string creatorName, IEnumerable<EmailResultModel> emailResultsHistory)
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

    private static string GetMessageVoidRequestSuccess(int requestId, int matchId)
    {
        return matchId > 0 ? $"Request ID: {requestId} Match ID: {matchId} voided successfully!" : $"Request ID: {requestId} voided successfully!";
    }
}