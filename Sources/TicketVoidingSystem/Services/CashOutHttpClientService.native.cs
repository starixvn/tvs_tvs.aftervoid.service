using System;
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
    public interface CashOutHttpClientService
    {
        Task VoidBet(CashOutApiModel requestModel);
    }

    [ExcludeFromCodeCoverage]
    public class CashOutHttpClientServiceImp : CashOutHttpClientService
    {
        private readonly HttpClient httpClient;
        private readonly string jackTimeCashOutUrl;

        public CashOutHttpClientServiceImp(HttpClient client, IConfiguration configuration)
        {
            jackTimeCashOutUrl = configuration.GetSection("JackTimeCashOutUrl").Value;
            httpClient = client;
            httpClient.BaseAddress = new Uri(jackTimeCashOutUrl);
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

        public async Task VoidBet(CashOutApiModel requestModel)
        {
            const string actionName = "VoidBet";
            using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(requestModel));
            if (response.IsSuccessStatusCode)
            {
                await LogService.InfoAsync(await CreateLogResponse(actionName, response));
                return;
            }

            await LogService.ErrorAsync(await CreateLogResponse(actionName, response));
        }

        private async Task<string> CreateLogResponse(string actionName, HttpResponseMessage response)
        {
            var requestContent = response.RequestMessage.Content != null ? await response.RequestMessage.Content.ReadAsStringAsync() : string.Empty;
            var responseContent = await response.Content.ReadAsStringAsync();

            return $"{jackTimeCashOutUrl}{actionName}" +
                   $"{Environment.NewLine}RequestContent:{Environment.NewLine}{requestContent}" +
                   $"{Environment.NewLine}Response:{Environment.NewLine}{response}" +
                   $"{Environment.NewLine}ResponseContent:{Environment.NewLine}{responseContent}";
        }
    }
}