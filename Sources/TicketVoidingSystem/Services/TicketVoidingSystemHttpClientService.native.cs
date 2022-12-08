using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fanex.Logging;
using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Models;

namespace TicketVoidingSystem.Services
{
    public interface TicketVoidingSystemHttpClientService
    {
        Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status);

        Task SignalRNotifyErrorMessageToUser(string userName, string message);

        Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results);

        Task<string> BetListGetSportTypeName(int sportType, string languageCode);

        Task<(bool, IEnumerable<Result>)> SendEmail(IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason voidedReason, Message message,
            int issueTypeId, string negativeBalanceUsers);

        Task<(bool, byte[])> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds);
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
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { status }));
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
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { userName, message }));
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
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { userName, results }));
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
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { sportType, languageCode }));
            if (response.IsSuccessStatusCode)
            {
                return await DeserializeResponseStream<string>(response);
            }

            await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return string.Empty;
        }

        public async Task<(bool, IEnumerable<Result>)> SendEmail(IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason voidedReason, Message message,
            int issueTypeId, string negativeBalanceUsers)
        {
            const string actionName = "SendEmail/Send";
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { emailTickets, match, voidedReason, message, issueTypeId, negativeBalanceUsers }));
            if (response.IsSuccessStatusCode)
            {
                await LogResponseStream(response);
                var responseData = await DeserializeResponseStream<IEnumerable<Result>>(response);
                return responseData != null ? (true, responseData) : (false, null);
            }

            await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return (false, null);
        }

        public async Task<(bool, byte[])> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId, string voidingHistoryIds)
        {
            const string actionName = "SendLetstalk/GetContentExcelVoidedTicket";
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { requestId, matchId, issueTypeId, voidingHistoryIds }));
            if (response.IsSuccessStatusCode)
            {
                var responseData = await DeserializeResponseStream<byte[]>(response);
                return (true, responseData);
            }

            await LogService.ErrorAsync($"{ticketVoidingSystemUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return (false, null);
        }

        private StringContent CreateRequestStringContent<T>(T data)
        {
            var dataJson = new StringContent(
                JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                Encoding.UTF8,
                MediaTypeNames.Application.Json);

            return dataJson;
        }

        private static async Task<T> DeserializeResponseStream<T>(HttpResponseMessage response)
        {
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            return await JsonSerializer.DeserializeAsync<T>(responseStream, options);
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
}