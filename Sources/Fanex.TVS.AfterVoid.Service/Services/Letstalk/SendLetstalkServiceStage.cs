using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Service.Criteria;
using Fanex.TVS.AfterVoid.Service.Models;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Service.Services.Letstalk;

public interface SendLetstalkServiceStage
{
    Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk();

    Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds);

    Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList);

    Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds);

    Task WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime);

    Task InsertLetstalk(int requestId, string broadcastSessionId, int statusId);

    Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId);
}

public class SendLetstalkServiceStageImp : SendLetstalkServiceStage
{
    private readonly IDynamicRepository repository;
    private readonly LoggingService loggingService;
    private readonly VoidBetAutomationService voidBetAutomationService;
    private readonly TVSNewSiteHttpClientService tvsNewSiteHttpClientService;

    public SendLetstalkServiceStageImp(
        IDynamicRepository repository,
        LoggingService loggingService,
        VoidBetAutomationService voidBetAutomationService,
        TVSNewSiteHttpClientService tvsNewSiteHttpClientService)
    {
        this.repository = repository;
        this.loggingService = loggingService;
        this.voidBetAutomationService = voidBetAutomationService;
        this.tvsNewSiteHttpClientService = tvsNewSiteHttpClientService;
    }

    public async Task<IEnumerable<VoidBetAutomationInfo>> GetVoidBetAutomationRunLetstalk()
    {
        var criteria = new GetAutomationInfoLetstalkCriteria();

        return await repository.FetchAsync<VoidBetAutomationInfo>(criteria);
    }

    public async Task<byte[]> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds)
    {
        var (isSentEmail, results) = await tvsNewSiteHttpClientService.GetExcelVoidedTicketForSendLetstalk(requestId, matchId, issueTypeId, voidingHistoryIds);
        if (isSentEmail)
        {
            return results;
        }

        throw new ExternalException(nameof(GetContentExcelVoidedTicket));
    }

    public async Task<IEnumerable<TicketEmailModel>> GetVoidedTickets(IEnumerable<VoidBetAutomationInfo> voidBetAutomationList)
    {
        var voidedTickets = new List<TicketEmailModel>();
        foreach (var item in voidBetAutomationList)
        {
            var ticketsInServer =
                await voidBetAutomationService.GetVoidedTicketsByVoidingHistory(item.VoidingHistoryId, item.SiteDbGroupId, false);
            voidedTickets.AddRange(ticketsInServer);
        }

        return voidedTickets;
    }

    public async Task<IEnumerable<VoidDataXmlModel>> GetVoidDataXml(IEnumerable<int> voidingHistoryIds)
    {
        var allVoidDataInCurrentVoidTimes = await GetAllVoidDataInCurrentVoidTimes(voidingHistoryIds);
        return allVoidDataInCurrentVoidTimes.Where(x => voidingHistoryIds.Contains(x.VoidingHistoryId));
    }

    private async Task<IEnumerable<VoidDataXmlModel>> GetAllVoidDataInCurrentVoidTimes(IEnumerable<int> voidingHistoryIds)
    {
        var criteria = new GetVoidingHistoryCriteria
        {
            VoidingHistoryIds = string.Join(",", voidingHistoryIds)
        };

        return await repository.FetchAsync<VoidDataXmlModel>(criteria);
    }

    public async Task WriteLogLetstalk(IEnumerable<Result> results, GroupVoidBetAutomationInfo group, Stopwatch startTime)
    {
        var logBuilder = new StringBuilder();
        logBuilder.Append($"SendLetstalk - RequestId: {group.RequestId} - " +
                          $"BroadcastSessionId: {group.BroadcastSessionId} - Execute Time: {startTime.ElapsedMilliseconds} ms \n");
        foreach (var result in results)
        {
            var log = $"Code: {result.ErrorCode} - Message: {result.ErrorMsg}\n";
            logBuilder.Append(log);
        }

        await loggingService.InfoAsync(logBuilder.ToString());
    }

    public async Task InsertLetstalk(int requestId, string broadcastSessionId, int statusId)
    {
        var command = new InsertLetstalkCommand
        {
            RequestId = requestId,
            BroadcastSessionId = broadcastSessionId,
            StatusId = statusId
        };

        await repository.ExecuteAsync(command);
    }

    public async Task UpdateLetstalk(int requestId, string broadcastSessionId, int statusId)
    {
        var command = new UpdateLetstalkCommand
        {
            RequestId = requestId,
            BroadcastSessionId = broadcastSessionId,
            StatusId = statusId
        };

        await repository.ExecuteAsync(command);
    }
}