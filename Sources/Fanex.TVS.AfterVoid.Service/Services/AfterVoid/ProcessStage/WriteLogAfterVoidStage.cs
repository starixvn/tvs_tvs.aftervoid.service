using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.AfterVoid.ProcessStage;

public interface WriteLogAfterVoidStage
{
    Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results);
}

public class WriteLogAfterVoidStageImp(LoggingService loggingService) : WriteLogAfterVoidStage
{
    public async Task Execute(VoidBetAutomationInfo automationInfo, IEnumerable<Result> results)
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
}