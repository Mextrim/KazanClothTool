using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

public static class UpdateHelper
{
    private static readonly string ExecutableLocation = GetExecutableLocation();

    public static string GetCurrentVersion()
    {
        string version = FileVersionInfo.GetVersionInfo(ExecutableLocation).FileVersion;
        return string.IsNullOrWhiteSpace(version)
            ? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"
            : version;
    }

    // KazanClothTool intentionally does not download or execute updates from a third-party repository.
    // Distribution updates are performed by replacing the published application folder.
    public static Task CheckForUpdates()
    {
        return Task.CompletedTask;
    }

    private static string GetExecutableLocation()
    {
        string assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "KazanClothTool";
        string executablePath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.exe");

        return File.Exists(executablePath)
            ? executablePath
            : Assembly.GetExecutingAssembly().Location;
    }
}
