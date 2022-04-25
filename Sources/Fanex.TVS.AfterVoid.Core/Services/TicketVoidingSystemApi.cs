using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface TicketVoidingSystemApi
    {
        HttpClient GetHttpClient();

        Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status);

        Task SignalRNotifyErrorMessageToUser(string userName, string message);

        Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results);

        Task<string> BetListGetSportTypeName(int sportType, string languageCode);

        Task<(bool, IEnumerable<Result>)> SendEmail(IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason voidedReason, Message message,
            int issueTypeId, string negativeBalanceUsers);
    }

    public class TicketVoidingSystemApiImp : TicketVoidingSystemApi
    {
        private static readonly HttpClient httpClient = new();
        private readonly LoggingService loggingService;

        public TicketVoidingSystemApiImp(LoggingService loggingService)
        {
            SetupClientDefault();
            this.loggingService = loggingService;
        }

        private static void SetupClientDefault()
        {
            httpClient.BaseAddress = new Uri(AppConfigService.AppConfig.TicketVoidingSystemUrl);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public HttpClient GetHttpClient()
        {
            return httpClient;
        }

        public async Task SignalRUpdateRequestStatusToAllUsers(IEnumerable<RequestStatus> status)
        {
            const string actionName = "SignalRApi/UpdateRequestStatusToAllUsers";
            var response = await httpClient.PostAsJsonAsync(actionName, new { status });
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            loggingService.Error($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                 $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        }

        public async Task SignalRNotifyErrorMessageToUser(string userName, string message)
        {
            const string actionName = "SignalRApi/NotifyErrorMessageToUser";
            var response = await httpClient.PostAsJsonAsync(actionName, new { userName, message });
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            loggingService.Error($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                 $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        }

        public async Task SignalRNotifyVoidResultToUser(string userName, IEnumerable<Result> results)
        {
            const string actionName = "SignalRApi/NotifyVoidResultToUser";
            var response = await httpClient.PostAsJsonAsync(actionName, new { userName, results });
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            loggingService.Error($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                 $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
        }

        public async Task<string> BetListGetSportTypeName(int sportType, string languageCode)
        {
            const string actionName = "BetListCore/GetSportTypeName";
            var response = await httpClient.PostAsJsonAsync(actionName, new { sportType, languageCode });
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsAsync<string>();
                return responseData;
            }

            loggingService.Error($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                 $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return string.Empty;
        }

        public async Task<(bool, IEnumerable<Result>)> SendEmail(IEnumerable<TicketEmailModel> emailTickets, Match match, VoidedReason voidedReason, Message message,
            int issueTypeId, string negativeBalanceUsers)
        {
            const string actionName = "SendEmail/Send";
            var response = await httpClient.PostAsJsonAsync(actionName, new { emailTickets, match, voidedReason, message, issueTypeId, negativeBalanceUsers });
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsAsync<IEnumerable<Result>>();
                return (true, responseData);
            }

            loggingService.Error($"{AppConfigService.AppConfig.TicketVoidingSystemUrl}{actionName}" +
                                 $"{Environment.NewLine}Response:{Environment.NewLine}{response}");
            return (false, null);
        }
    }
}