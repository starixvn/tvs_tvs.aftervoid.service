using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fanex.Logging;
using Fanex.TVS.AfterVoid.Core.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface TicketVoidingSystemApi
    {
        Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status);

        Task SignalRNotifyErrorMessageToUser(string userName, string message);

        Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results);

        Task<string> BetListGetSportTypeName(int sportType, string languageCode);

        Task<(bool, IEnumerable<Result>)> SendEmail(IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason voidedReason, Message message,
            int issueTypeId, string negativeBalanceUsers);

        Task<(bool, byte[])> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId);
    }

    [ExcludeFromCodeCoverage]
    public class TicketVoidingSystemApiImp : TicketVoidingSystemApi
    {
        private readonly HttpClient httpClient;

        public TicketVoidingSystemApiImp(HttpClient client)
        {
            httpClient = client;
            httpClient.BaseAddress = new Uri(AppConfigService.AppConfig.TicketVoidingSystemUrl);
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
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
            return await JsonSerializer.DeserializeAsync<T>(responseStream);
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

        public async Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status)
        {
            const string actionName = "SignalRApi/UpdateRequestStatusToAllUsers";
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { status }));
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
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

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
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

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
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

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
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
                if (responseData != null && responseData.Any())
                {
                    return (true, responseData);
                }

                return (false, null);
            }

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return (false, null);
        }

        public async Task<(bool, byte[])> GetContentExcelVoidedTicket(int requestId, int matchId, int issueTypeId)
        {
            const string actionName = "SendLetstalk/GetContentExcelVoidedTicket";
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(new { requestId, matchId, issueTypeId }));
            if (response.IsSuccessStatusCode)
            {
                var responseData = await DeserializeResponseStream<byte[]>(response);
                return (true, responseData);
            }

            await LogService.ErrorAsync($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                        $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return (false, null);
        }
    }
}