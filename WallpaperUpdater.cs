using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace rans0m;

internal static class WallpaperUpdater
{
    private sealed class Backup { public string Original { get; set; } = ""; public string Temporary { get; set; } = ""; }
    private const uint SPI_SETDESKWALLPAPER = 20;
    private const uint SPIF_UPDATEINIFILE = 1;
    private const uint SPIF_SENDCHANGE = 2;
    private static readonly string Base = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rans0mAwarenessLab");
    private static readonly string BackupPath = Path.Combine(Base, "wallpaper-backup.json");

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, string value, uint flags);

    public static bool SetDarkRedWallpaper()
    {
        if (!Config.ChangeWallpaper) return false;
        try
        {
            Directory.CreateDirectory(Base);
            // Never replace a pending previous backup; that would lose the original path.
            if (File.Exists(BackupPath)) return false;
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            string? original = key?.GetValue("Wallpaper")?.ToString();
            if (string.IsNullOrWhiteSpace(original)) return false;
            string temp = Path.Combine(Base, "wallpaper-" + Guid.NewGuid().ToString("N") + ".bmp");
            using (var bmp = new Bitmap(1920, 1080))
            {
                using (var graphics = Graphics.FromImage(bmp)) graphics.Clear(Color.FromArgb(50, 0, 0));
                bmp.Save(temp, ImageFormat.Bmp);
            }
            // Persist the recovery information BEFORE any OS change.
            using (var stream = new FileStream(BackupPath, FileMode.CreateNew, FileAccess.Write))
                JsonSerializer.Serialize(stream, new Backup { Original = original, Temporary = temp });
            if (SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, temp, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)) return true;
            RestoreWallpaper();
        }
        catch { /* Wallpaper is an optional visual effect. */ }
        return false;
    }

    public static void RestoreWallpaper()
    {
        try
        {
            if (!File.Exists(BackupPath)) return;
            Backup? b = JsonSerializer.Deserialize<Backup>(File.ReadAllText(BackupPath));
            if (b == null || string.IsNullOrWhiteSpace(b.Original)) return;
            // Retain backup for recovery if restoration fails.
            if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, b.Original,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)) return;
            if (Path.GetDirectoryName(b.Temporary)?.Equals(Base, StringComparison.OrdinalIgnoreCase) == true
                && Path.GetFileName(b.Temporary).StartsWith("wallpaper-", StringComparison.OrdinalIgnoreCase))
            { try { File.Delete(b.Temporary); } catch { } }
            File.Delete(BackupPath);
        }
        catch { }
    }
}
