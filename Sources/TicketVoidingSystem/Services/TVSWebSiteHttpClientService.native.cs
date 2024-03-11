using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Fanex.Logging;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Models.SendEmailRecover;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Services;

public interface TVSWebSiteHttpClientService
{
    Task<string> GetSportTypeNameFromBetList(int sportType, string languageCode);

    Task<(bool, byte[])> GetExcelVoidedTicketForSendLetstalk(int requestId, int matchId, int issueTypeId,
        string voidingHistoryIds);

    Task<(bool, IEnumerable<Result>)> SendEmail(SendEmailApiModel model);

    Task<(bool, IEnumerable<Result>)> SendEmailRecover(SendEmailRecoverApiModel model);
}

public class TVSWebSiteHttpClientServiceImp : TVSWebSiteHttpClientService
{
    private readonly HttpClient httpClient;
    private readonly LoggingService loggingService;

    public TVSWebSiteHttpClientServiceImp(HttpClient client, LoggingService loggingService)
    {
        httpClient = client;
        httpClient.BaseAddress = new Uri(AppSettingService.AppSetting.TVSWebSiteUrl);
        httpClient.Timeout = TimeSpan.FromMinutes(15);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
        this.loggingService = loggingService;
    }

    public async Task<string> GetSportTypeNameFromBetList(int sportType, string languageCode)
    {
        const string actionName = "api/SharedApi/GetSportTypeName";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { sportType, languageCode }));
        if (response.IsSuccessStatusCode)
        {
            return await Helper.DeserializeResponseStream<string>(response);
        }

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return string.Empty;
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

        await loggingService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
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

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return (false, null);
    }

    public async Task<(bool, IEnumerable<Result>)> SendEmailRecover(SendEmailRecoverApiModel model)
    {
        const string actionName = "api/SendEmail/SendEmailRecoverTickets";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(model));
        if (response.IsSuccessStatusCode)
        {
            await LogResponseStream(response);
            var responseData = await Helper.DeserializeResponseStream<IEnumerable<Result>>(response);
            return responseData != null ? (true, responseData) : (false, null);
        }

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
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