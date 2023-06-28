using System;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Services;

public interface TVSNewSiteHttpClientService
{
    Task<(bool, byte[])> GetExcelVoidedTicketForSendLetstalk(int requestId, int matchId, int issueTypeId,
        string voidingHistoryIds);
}

public class TVSNewSiteHttpClientServiceImp : TVSNewSiteHttpClientService
{
    private readonly HttpClient httpClient;
    private readonly LoggingService loggingService;

    public TVSNewSiteHttpClientServiceImp(HttpClient client, LoggingService loggingService)
    {
        httpClient = client;
        httpClient.BaseAddress = new Uri(AppSettingService.AppSetting.TVSNewSiteUrl);
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        this.loggingService = loggingService;
    }

    public async Task<(bool, byte[])> GetExcelVoidedTicketForSendLetstalk(int requestId, int matchId, int issueTypeId, string voidingHistoryIds)
    {
        const string actionName = "api/ManageRequest/GetExcelVoidedTicketForSendLetstalk";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { requestId, matchId, issueTypeId, voidingHistoryIds }));
        if (response.IsSuccessStatusCode)
        {
            var responseData = await Helper.DeserializeResponseStream<byte[]>(response);
            return (true, responseData);
        }

        await loggingService.ErrorAsync($"{AppSettingService.AppSetting.TVSNewSiteUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return (false, null);
    }
}