using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StreamingTools.Updates.Json;

namespace StreamingTools.Updates;

public class UpdateManager {
    public static async Task<GithubLatestReleaseJson?> GetLatestVersion() {
        var handler = new HttpClientHandler();
        handler.AutomaticDecompression = ~DecompressionMethods.None;
        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, Constants.APP_UPDATE_API);
        request.Headers.TryAddWithoutValidation("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) {
            return null;
        }
        
        string body = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GithubLatestReleaseJson>(body);
    }
}