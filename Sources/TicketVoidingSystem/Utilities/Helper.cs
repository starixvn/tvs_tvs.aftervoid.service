using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TicketVoidingSystem.Constants;

namespace TicketVoidingSystem.Utilities;

public static class Helper
{
    public static string GetSettingKeyBySiteDbGroupId(int siteDbGroupId, string spName)
    {
        var serverName = SiteDbGroupConstant.ServerList.First(x => x.Id == siteDbGroupId).ServerName;

        return $"{serverName}.{spName}";
    }

    public static StringContent CreateRequestStringContent<T>(T data)
    {
        var dataJson = new StringContent(
            JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        return dataJson;
    }

    public static async Task<T> DeserializeResponseStream<T>(HttpResponseMessage response)
    {
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return await JsonSerializer.DeserializeAsync<T>(responseStream, options);
    }
}