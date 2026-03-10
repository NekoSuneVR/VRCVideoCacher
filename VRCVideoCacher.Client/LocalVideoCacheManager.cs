using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace VRCVideoCacher;

public static class LocalVideoCacheManager
{
    private static readonly ILogger Log = Program.Logger.ForContext(typeof(LocalVideoCacheManager));
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public static string CachePath
    {
        get
        {
            var configuredPath = ConfigManager.Config.LocalVideoCachePath;
            var path = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(Program.DataPath, "CachedVideos")
                : configuredPath;

            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static async Task<string> GetOrCreateCachedVideoUrl(string sourceUrl, string proxyUrl)
    {
        try
        {
            var cacheKey = GetCacheKey(sourceUrl);
            var existingFile = Directory.GetFiles(CachePath, $"{cacheKey}.*").FirstOrDefault();
            if (!string.IsNullOrEmpty(existingFile))
                return BuildLocalUrl(Path.GetFileName(existingFile));

            var tempPath = Path.Combine(CachePath, $"{cacheKey}.download");
            using var response = await HttpClient.GetAsync(proxyUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Local cache download failed with status {StatusCode}", response.StatusCode);
                return string.Empty;
            }

            var extension = GetFileExtension(response);
            var finalFileName = $"{cacheKey}{extension}";
            var finalPath = Path.Combine(CachePath, finalFileName);

            await using (var input = await response.Content.ReadAsStreamAsync())
            await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await input.CopyToAsync(output);
            }

            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(tempPath, finalPath, true);
            Log.Information("Cached video locally: {Path}", finalPath);
            return BuildLocalUrl(finalFileName);
        }
        catch (Exception ex)
        {
            Log.Warning("Local video cache failed: {Error}", ex.Message);
            return string.Empty;
        }
    }

    private static string BuildLocalUrl(string fileName)
    {
        return $"{ConfigManager.Config.ytdlWebServerURL}/cache/{Uri.EscapeDataString(fileName)}";
    }

    private static string GetCacheKey(string sourceUrl)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceUrl))).ToLowerInvariant();
    }

    private static string GetFileExtension(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
        if (contentType == "video/webm")
            return ".webm";
        if (contentType == "audio/mpeg")
            return ".mp3";
        if (contentType == "audio/mp4")
            return ".m4a";
        if (contentType == "audio/webm")
            return ".weba";
        if (contentType == "video/mp4")
            return ".mp4";

        var path = response.RequestMessage?.RequestUri?.AbsolutePath;
        var extension = Path.GetExtension(path);
        return string.IsNullOrWhiteSpace(extension) ? ".mp4" : extension;
    }
}
