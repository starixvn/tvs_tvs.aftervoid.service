using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Fanex.Logging;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Services;

public interface TVSNewSiteHttpClientService
{
    Task<(bool, byte[])> GetExcelVoidedTicketForSendLetstalk(int requestId, int matchId, int issueTypeId,
        string voidingHistoryIds);

    Task<(bool, IEnumerable<Result>)> SendEmail(SendEmailApiModel model);
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

    public async Task<(bool, IEnumerable<Result>)> SendEmail(SendEmailApiModel model)
    {
        var actionName = model.IssueTypeId is IssueTypeConstant.DuplicateTickets
            ? "api/SendEmail/SendEmailDuplicateTickets"
            : "api/SendEmail/Send";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(model));
        if (response.IsSuccessStatusCode)
        {
            await LogResponseStream(response);
            var responseData = await Helper.DeserializeResponseStream<IEnumerable<Result>>(response);
            return responseData != null ? (true, responseData) : (false, null);
        }

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSNewSiteUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return (false, null);
    }

    private static async Task LogResponseStream(HttpResponseMessage response)
    {
        try
        {
            var responseString = await response.Content.ReadAsStringAsync();
            await LogService.InfoAsync("LogResponseStream" +
                                       $"{Environment.NewLine}Response:{Environment.NewLine}{responseString}");
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("LogResponseStream", e);
        }
    }
}