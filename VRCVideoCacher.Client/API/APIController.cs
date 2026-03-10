using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace VRCVideoCacher.API;

public class ApiController : WebApiController
{
    private static readonly Serilog.ILogger Log = Program.Logger.ForContext<ApiController>();
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    
    [Route(HttpVerbs.Get, "/getvideo")]
    public async Task GetVideo()
    {
        // escape double quotes for our own safety
        var requestUrl = Request.QueryString["url"]?.Replace("\"", "%22").Trim();
        
        if (string.IsNullOrEmpty(requestUrl))
        {
            Log.Error("No URL provided.");
            await HttpContext.SendStringAsync("No URL provided.", "text/plain", Encoding.UTF8);
            return;
        }

        Log.Information("Request URL: {URL}", requestUrl);

        if (requestUrl.StartsWith("https://dmn.moe"))
        {
            requestUrl = requestUrl.Replace("/sr/", "/yt/");
            Log.Information("YTS URL detected, modified to: {URL}", requestUrl);
        }

        if (ConfigManager.Config.BlockedUrls.Any(blockedUrl => requestUrl.StartsWith(blockedUrl)))
        {
            Log.Warning("URL Is Blocked: {url}", requestUrl);
            requestUrl = ConfigManager.Config.BlockRedirect;
        }

        var proxyUrl = await BuildVideoBypassUrl(requestUrl);
        if (ConfigManager.Config.LocalVideoCacheEnabled)
        {
            var localCacheUrl = await LocalVideoCacheManager.GetOrCreateCachedVideoUrl(requestUrl, proxyUrl);
            if (!string.IsNullOrEmpty(localCacheUrl))
            {
                Log.Information("Responding with local cached URL: {URL}", localCacheUrl);
                await HttpContext.SendStringAsync(localCacheUrl, "text/plain", Encoding.UTF8);
                return;
            }
        }

        Log.Information("Responding with proxied URL: {URL}", proxyUrl);
        await HttpContext.SendStringAsync(proxyUrl, "text/plain", Encoding.UTF8);
    }

    private static async Task<string> BuildVideoBypassUrl(string requestUrl)
    {
        var candidates = ConfigManager.Config.VideoBypassBaseUrls.Length > 0
            ? ConfigManager.Config.VideoBypassBaseUrls
            : ["https://dl.nekosunevr.co.uk", "https://dl.ballisticok.xyz"];

        foreach (var baseUrl in candidates)
        {
            var proxyUrl = $"{baseUrl}/api/videobypass?url={Uri.EscapeDataString(requestUrl)}";
            if (await IsEndpointAvailable(proxyUrl))
                return proxyUrl;
        }

        var fallbackBaseUrl = candidates[0];
        return $"{fallbackBaseUrl}/api/videobypass?url={Uri.EscapeDataString(requestUrl)}";
    }

    private static async Task<bool> IsEndpointAvailable(string proxyUrl)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, proxyUrl);
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if ((int)response.StatusCode < 500)
                return true;
        }
        catch (Exception ex)
        {
            Log.Warning("Video bypass endpoint probe failed: {Error}", ex.Message);
        }

        return false;
    }
}
