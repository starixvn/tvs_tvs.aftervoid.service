using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Fanex.Logging;
using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Models;
using TicketVoidingSystem.Utilities;

namespace TicketVoidingSystem.Services;

public interface TicketVoidingSystemHttpClientService
{
    Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status);

    Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results);

    Task<string> BetListGetSportTypeName(int sportType, string languageCode);
}

[ExcludeFromCodeCoverage]
public class TicketVoidingSystemHttpClientServiceImp : TicketVoidingSystemHttpClientService
{
    private readonly HttpClient httpClient;
    private readonly string ticketVoidingSystemUrl;

    public TicketVoidingSystemHttpClientServiceImp(HttpClient client, IConfiguration configuration)
    {
        ticketVoidingSystemUrl = configuration.GetSection("TicketVoidingSystemUrl").Value;
        httpClient = client;
        httpClient.BaseAddress = new Uri(ticketVoidingSystemUrl);
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
    }

    public async Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status)
    {
        const string actionName = "SignalRApi/UpdateRequestStatusToAllUsers";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { status }));
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
    }

    public async Task SignalRNotifyErrorMessageToUser(string userName, string message)
    {
        const string actionName = "SignalRApi/NotifyErrorMessageToUser";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { userName, message }));
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
    }

    public async Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results)
    {
        const string actionName = "SignalRApi/NotifyVoidResultToUser";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { userName, results }));
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
    }

    public async Task<string> BetListGetSportTypeName(int sportType, string languageCode)
    {
        const string actionName = "BetListCore/GetSportTypeName";
        using var response = await httpClient.PostAsync(actionName, Helper.CreateRequestStringContent(new { sportType, languageCode }));
        if (response.IsSuccessStatusCode)
        {
            return await Helper.DeserializeResponseStream<string>(response);
        }

        await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                    $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        return string.Empty;
    }
}