using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Resources;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface NotifyVoidResultsSignalRStage
{
    Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults, string creatorName, IEnumerable<EmailResultModel> emailResultsHistory);
}

public class NotifyVoidResultsSignalRStageImp : NotifyVoidResultsSignalRStage
{
    private readonly NotificationSignalRService notificationSignalRService;

    public NotifyVoidResultsSignalRStageImp(NotificationSignalRService notificationSignalRService)
    {
        this.notificationSignalRService = notificationSignalRService;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults, string creatorName, IEnumerable<EmailResultModel> emailResultsHistory)
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