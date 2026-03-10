using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Serilog;

namespace VRCVideoCacher;

public class WinGet
{
    private static readonly ILogger Log = Program.Logger.ForContext<WinGet>();
    private const string WingetExe = "winget.exe";
    private static bool _sourceUnavailableLogged;
    private static bool _skipWingetChecks;
    private static readonly Dictionary<string, string> WingetPackages = new()
    {
        { "VP9 Video Extensions", "9n4d0msmp0pt" },
        { "AV1 Video Extension", "9mvzqvxjbq9v" },
        { "Dolby Digital Plus decoder for PC OEMs", "9nvjqjbdkn97" }
    };
    
    [SupportedOSPlatform("windows")]
    public static async Task TryInstallPackages()
    {
        if (!ConfigManager.Config.AutoInstallCodecs)
        {
            Log.Information("Codec auto-install is disabled.");
            return;
        }

        if (_skipWingetChecks)
            return;

        Log.Information("Checking for missing codec packages...");
        if (!IsOurPackagesInstalled())
        {
            if (_skipWingetChecks)
                return;
            Log.Information("Installing missing codec packages...");
            await InstallAllPackages();
        }
    }

    private static bool IsOurPackagesInstalled()
    {
        foreach (var package in WingetPackages.Values)
        {
            if (!IsPackageInstalled(package))
            {
                return false;
            }
        }

        Log.Information("Codec packages are already installed.");
        return true;
    }

    private static bool IsPackageInstalled(string packageId)
    {
        try
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = WingetExe,
                    Arguments = $"list \"{packageId}\" -s msstore --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (ShouldSkipWinget(error) || ShouldSkipWinget(output))
            {
                LogWingetUnavailable(error, output);
                return false;
            }
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return false;
        }
    }

    private static async Task InstallAllPackages()
    {
        foreach (var package in WingetPackages.Values)
        {
            await InstallPackage(package);
        }
    }

    private static async Task InstallPackage(string packageId)
    {
        try
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = WingetExe,
                    Arguments = $"install --id {packageId} -s msstore --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };
            process.Start();
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrEmpty(line.Trim()))
                    Log.Debug("{Winget}: " + line, WingetExe);
            }
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (ShouldSkipWinget(error))
            {
                LogWingetUnavailable(error, string.Empty);
                return;
            }
            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                throw new Exception($"Installation failed with exit code {process.ExitCode}. Error: {error}");
            
            var packageName = WingetPackages.FirstOrDefault(x => x.Value == packageId).Key;
            if (process.ExitCode == 0)
                Log.Information("Successfully installed package: {packageName}", packageName);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    private static bool ShouldSkipWinget(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               (text.Contains("0x8a15005e", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("The server certificate did not match any of the expected values", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Failed when opening source(s)", StringComparison.OrdinalIgnoreCase));
    }

    private static void LogWingetUnavailable(string error, string output)
    {
        _skipWingetChecks = true;
        if (_sourceUnavailableLogged)
            return;

        var details = string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim();
        Log.Warning("Skipping codec auto-install because winget source access is broken. {Details}", details);
        _sourceUnavailableLogged = true;
    }
}
