using Newtonsoft.Json;
using Serilog;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace VRCVideoCacher;

public class ConfigManager
{
    public static readonly ConfigModel Config;
    private static readonly ILogger Log = Program.Logger.ForContext<ConfigManager>();
    private static readonly string ConfigFilePath;

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
        if (!string.IsNullOrWhiteSpace(Config.LocalVideoCachePath))
            Config.LocalVideoCachePath = Config.LocalVideoCachePath.Trim();
        Config.WebServerBindUrls ??= [];
        Config.VideoBypassBaseUrls ??= [];
        if (Config.WebServerBindUrls.Length > 0)
        {
            Config.WebServerBindUrls = Config.WebServerBindUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim().TrimEnd('/'))
                .ToArray();
        }
        if (Config.VideoBypassBaseUrls.Length > 0)
        {
            Config.VideoBypassBaseUrls = Config.VideoBypassBaseUrls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        
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
        Config.PatchResonite = false;
        Config.PatchVRC = true;
    }
}

// ReSharper disable InconsistentNaming
public class ConfigModel
{
    public string ytdlWebServerURL = "http://127.0.0.1:9696";
    public string[] WebServerBindUrls = ["http://127.0.0.1:9696"];
    public string[] BlockedUrls = ["https://na2.vrdancing.club/sampleurl.mp4"];
    public string BlockRedirect = "https://www.youtube.com/watch?v=byv2bKekeWQ";
    public bool PatchResonite = false;
    public bool PatchVRC = true;
    public bool AutoUpdate = true;
    public bool AutoInstallCodecs = false;
    public bool LocalVideoCacheEnabled = false;
    public string LocalVideoCachePath = "";
    public string[] VideoBypassBaseUrls =
    [
        "https://dl.nekosunevr.co.uk",
        "https://dl.ballisticok.xyz"
    ];
    
}
// ReSharper restore InconsistentNaming
