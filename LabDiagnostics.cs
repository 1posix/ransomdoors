using System.IO;
using System.Text;

namespace rans0m;

/// <summary>Minimal local diagnostics for a lab session; never records keystrokes.</summary>
internal static class LabDiagnostics
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rans0mAwarenessLab", "lab-events.log");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                // Prevent unlimited log growth after repeated tests.
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 1024 * 1024)
                    File.Move(LogPath, LogPath + ".old", overwrite: true);
                File.AppendAllText(LogPath, $"{DateTimeOffset.UtcNow:O}  {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch { /* Logging must never affect the game or its cleanup. */ }
    }
}
