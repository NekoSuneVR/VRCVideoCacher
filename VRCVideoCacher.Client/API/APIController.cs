using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using VRCVideoCacher.Models;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.API;

public class ApiController : WebApiController
{
    private static readonly Serilog.ILogger Log = Program.Logger.ForContext<ApiController>();
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    
    [Route(HttpVerbs.Post, "/youtube-cookies")]
    public async Task ReceiveYoutubeCookies()
    {
        using var reader = new StreamReader(HttpContext.OpenRequestStream(), Encoding.UTF8);
        var cookies = await reader.ReadToEndAsync();
        if (!Program.IsCookiesValid(cookies))
        {
            Log.Error("Invalid cookies received, maybe you haven't logged in yet, not saving.");
            HttpContext.Response.StatusCode = 400;
            await HttpContext.SendStringAsync("Invalid cookies.", "text/plain", Encoding.UTF8);
            return;
        }

        if (!RemoteServerProxy.IsEnabled)
        {
            HttpContext.Response.StatusCode = 503;
            await HttpContext.SendStringAsync("Remote server not configured.", "text/plain", Encoding.UTF8);
            return;
        }

        var (success, status, body) = await RemoteServerProxy.SendCookies(cookies);
        HttpContext.Response.StatusCode = status;
        await HttpContext.SendStringAsync(body, "text/plain", Encoding.UTF8);
        if (!success)
            Log.Warning("Failed to forward cookies to remote server.");
    }

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
