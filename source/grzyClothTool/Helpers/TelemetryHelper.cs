using System;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

/// <summary>
/// Telemetry is intentionally disabled in KazanClothTool. The helper remains as a
/// compatibility shim for older code paths and writes diagnostics locally only.
/// </summary>
public static class TelemetryHelper
{
    public static Task LogSession(bool isSessionStart)
    {
        return Task.CompletedTask;
    }

    public static void CaptureExceptionWithAttachment(Exception ex, string path)
    {
        LogHelper.Log($"Ошибка обработки файла: {ex.Message}", Views.LogType.Error);
        ErrorLogHelper.LogError("Ошибка обработки файла", ex);
    }
}
