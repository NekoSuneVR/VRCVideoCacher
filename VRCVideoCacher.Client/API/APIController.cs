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
        var avPro = string.Compare(Request.QueryString["avpro"], "true", StringComparison.OrdinalIgnoreCase) == 0;
        var source = Request.QueryString["source"] ?? "vrchat";
        
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

        var videoInfo = await VideoId.GetVideoId(requestUrl, avPro);
        if (videoInfo == null)
        {
            Log.Information("Failed to get Video Info for URL: {URL}", requestUrl);
            return;
        }

        if (videoInfo.UrlType != UrlType.YouTube)
        {
            Log.Information("Non-YouTube URL: bypassing.");
            await HttpContext.SendStringAsync(string.Empty, "text/plain", Encoding.UTF8);
            return;
        }

        if (!RemoteServerProxy.IsEnabled)
        {
            Log.Error("Remote server not configured for YouTube.");
            HttpContext.Response.StatusCode = 503;
            await HttpContext.SendStringAsync("Remote server not configured.", "text/plain", Encoding.UTF8);
            return;
        }

        var (remoteSuccess, status, body) = await RemoteServerProxy.GetVideo(requestUrl, avPro, source);
        if (remoteSuccess)
        {
            Log.Information("Responding with Remote URL: {URL}", body);
            await HttpContext.SendStringAsync(body, "text/plain", Encoding.UTF8);
            return;
        }

        Log.Error("All remote nodes failed to fetch URL.");
        HttpContext.Response.StatusCode = status;
        await HttpContext.SendStringAsync(body, "text/plain", Encoding.UTF8);
    }
}
