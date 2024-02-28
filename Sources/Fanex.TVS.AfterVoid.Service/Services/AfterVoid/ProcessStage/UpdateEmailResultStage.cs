using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Constants;
using Fanex.TVS.AfterVoid.Service.Extensions;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface UpdateEmailResultStage
{
    Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults);
}

public class UpdateEmailResultStageImp : UpdateEmailResultStage
{
    private readonly VoidBetAutomationService voidBetAutomationService;

    public UpdateEmailResultStageImp(VoidBetAutomationService voidBetAutomationService)
    {
        this.voidBetAutomationService = voidBetAutomationService;
    }

    public async Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> currentEmailResults)
    {
        var licenseeEmailResult = currentEmailResults.Where(x =>
            x.ErrorCode is ErrorCode.SendEmailToLicenseeSuccess or ErrorCode.SendEmailToLicenseeFail or ErrorCode.LicenseeSuccess or ErrorCode.LicenseeFail);
        if (licenseeEmailResult.Any())
        {
            await voidBetAutomationService.UpdateEmailResultByVoidingHistory(automationInfo.RequestId, automationInfo.VoidingHistoryId, licenseeEmailResult.ToVoidResultsXML());
        }
    }
}