using Newtonsoft.Json;
using Serilog;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace VRCVideoCacher;

public class ConfigManager
{
    public static readonly ConfigModel Config;
    private static readonly ILogger Log = Program.Logger.ForContext<ConfigManager>();
    private static readonly string ConfigFilePath;
    public static readonly string UtilsPath;

    static ConfigManager()
    {
        Log.Information("Loading config...");
        ConfigFilePath = Path.Combine(Program.DataPath, "Config.json");
        Log.Debug("Using config file path: {ConfigFilePath}", ConfigFilePath);

        if (!File.Exists(ConfigFilePath))
        {
            Config = new ConfigModel();
            FirstRun();
        }
        else
        {
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(ConfigFilePath)) ?? new ConfigModel();
        }
        if (Config.ytdlWebServerURL.EndsWith('/'))
            Config.ytdlWebServerURL = Config.ytdlWebServerURL.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(Config.YouTubePoTokenUrl))
            Config.YouTubePoTokenUrl = Config.YouTubePoTokenUrl.Trim().TrimEnd('/');
        Config.RemoteServerUrls ??= [];
        Config.WebServerBindUrls ??= [];
        if (Config.RemoteServerUrls.Length > 0)
        {
            Config.RemoteServerUrls = Config.RemoteServerUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim().TrimEnd('/'))
                .ToArray();
        }
        if (Config.WebServerBindUrls.Length > 0)
        {
            Config.WebServerBindUrls = Config.WebServerBindUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim().TrimEnd('/'))
                .ToArray();
        }

        UtilsPath = Path.GetDirectoryName(Config.ytdlPath) ?? string.Empty;
        if (!UtilsPath.EndsWith("Utils"))
            UtilsPath = Path.Combine(UtilsPath, "Utils");

        Directory.CreateDirectory(UtilsPath);
        
        Log.Information("Loaded config.");
        TrySaveConfig();
    }

    private static void TrySaveConfig()
    {
        var newConfig = JsonConvert.SerializeObject(Config, Formatting.Indented);
        var oldConfig = File.Exists(ConfigFilePath) ? File.ReadAllText(ConfigFilePath) : string.Empty;
        if (newConfig == oldConfig)
            return;
        
        Log.Information("Config changed, saving...");
        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(Config, Formatting.Indented));
        Log.Information("Config saved.");
    }
    
    private static void FirstRun()
    {
        Log.Information("First run detected, writing client defaults.");
        Config.ytdlUseCookies = false;
        Config.PatchResonite = false;
        Config.PatchVRC = true;
        Config.RemoteServerEnabled = true;
        Config.RemoteServerYouTubeOnly = true;
        Config.RemoteServerFallbackToLocal = false;
        Config.RemoteServerDisableLocalCache = true;
    }
}

// ReSharper disable InconsistentNaming
public class ConfigModel
{
    public string ytdlWebServerURL = "http://localhost:9696";
    public string ytdlPath = "Utils\\yt-dlp.exe";
    public bool ytdlUseCookies = false;
    public bool ytdlAutoUpdate = true;
    public string ytdlAdditionalArgs = string.Empty;
    public string ytdlDubLanguage = string.Empty;
    public int ytdlDelay = 0;
    public string YouTubePoTokenUrl = "";
    public string CachedAssetPath = "";
    public string[] WebServerBindUrls = ["http://127.0.0.1:9696"];
    public string[] BlockedUrls = ["https://na2.vrdancing.club/sampleurl.mp4"];
    public string BlockRedirect = "https://www.youtube.com/watch?v=byv2bKekeWQ";
    public bool CacheYouTube = true;
    public int CacheYouTubeMaxResolution = 2160;
    public int CacheYouTubeMaxLength = 120;
    public float CacheMaxSizeInGb = 0;
    public int CacheEvictUnusedMinutes = 0;
    public int CacheEvictIntervalMinutes = 0;
    public bool CachePyPyDance = false;
    public bool CacheVRDancing = false;
    public bool PatchResonite = false;
    public bool PatchVRC = true;
    public bool AutoUpdate = true;
    public string[] PreCacheUrls = [];
    public bool RemoteServerEnabled = true;
    public bool RemoteServerYouTubeOnly = true;
    public bool RemoteServerFallbackToLocal = false;
    public bool RemoteServerDisableLocalCache = true;
    public int RemoteServerTimeoutSeconds = 15;
    public string[] RemoteServerUrls = [];
    
}
// ReSharper restore InconsistentNaming
