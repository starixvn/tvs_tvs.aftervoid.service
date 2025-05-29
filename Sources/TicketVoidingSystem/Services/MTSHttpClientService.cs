using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TicketVoidingSystem.Models;
using Fanex.Logging;

namespace TicketVoidingSystem.Services;

public interface MTSHttpClientService
{
    Task<MTSApiResponse> VoidBetAsync(MTSApiRequest request);
}

public class MTSHttpClientServiceImp: MTSHttpClientService
{
    
    private readonly HttpClient httpClient;
    private readonly string mtsApiBaseUrl;

    public MTSHttpClientServiceImp(HttpClient client, IConfiguration configuration)
    {
        mtsApiBaseUrl = configuration.GetSection("MTSApiBaseUrl").Value ?? string.Empty;
        httpClient = client;
        httpClient.BaseAddress = new Uri(mtsApiBaseUrl);
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Json);
    }
    
    public async Task<MTSApiResponse> VoidBetAsync(MTSApiRequest request)
    {
        const string actionName = "api/bet/void";
        using var response = await httpClient.PostAsync(actionName, CreateRequestStringContent(request));
        if (response.IsSuccessStatusCode)
        {
            await LogService.InfoAsync(await CreateLogResponse(actionName, response));
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MTSApiResponse>(responseContent);
        }
        await LogService.ErrorAsync(await CreateLogResponse(actionName, response));
        var errorContent = await response.Content.ReadAsStringAsync();
        return new MTSApiResponse { Success = false, Message = errorContent };
    }

    private static StringContent CreateRequestStringContent<T>(T data)
    {
        return new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
    }

    private async Task<string> CreateLogResponse(string actionName, HttpResponseMessage response)
    {
        var requestContent = response.RequestMessage?.Content != null ? await response.RequestMessage.Content.ReadAsStringAsync() : string.Empty;
        var responseContent = await response.Content.ReadAsStringAsync();
        return $"{mtsApiBaseUrl}{actionName}" +
               $"{Environment.NewLine}RequestContent:{Environment.NewLine}{requestContent}" +
               $"{Environment.NewLine}Response:{Environment.NewLine}{response}" +
               $"{Environment.NewLine}ResponseContent:{Environment.NewLine}{responseContent}";
    }
} 