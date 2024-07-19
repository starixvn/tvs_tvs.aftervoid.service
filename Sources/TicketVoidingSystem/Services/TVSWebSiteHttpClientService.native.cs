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
    Task<ContentByLanguageModel> GetSpecialBetTypeContent(IEnumerable<TicketEmailModel> tickets);

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

    public async Task<ContentByLanguageModel> GetSpecialBetTypeContent(IEnumerable<TicketEmailModel> tickets)
    {
        const string actionName = "api/MessageTemplate/GetSpecialBetTypeContent";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { tickets }));
        if (response.IsSuccessStatusCode)
        {
            await LogResponseStream(response, actionName);
            return await Helper.DeserializeResponseStream<ContentByLanguageModel>(response);
        }

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return null;
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
            await LogResponseStream(response, actionName);
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
            await LogResponseStream(response, actionName);
            var responseData = await Helper.DeserializeResponseStream<IEnumerable<Result>>(response);
            return responseData != null ? (true, responseData) : (false, null);
        }

        await LogService.ErrorAsync($"{AppSettingService.AppSetting.TVSWebSiteUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return (false, null);
    }

    private static async Task LogResponseStream(HttpResponseMessage response, string url)
    {
        try
        {
            var responseString = await response.Content.ReadAsStringAsync();
            await LogService.InfoAsync($"LogResponseStream: {url}" +
                                       $"{Environment.NewLine}Response:{Environment.NewLine}{responseString}");
        }
        catch (Exception e)
        {
            await LogService.ErrorAsync("LogResponseStream", e);
        }
    }
}