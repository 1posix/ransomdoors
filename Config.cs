using System.IO;
using System.Text.Json;

namespace rans0m;

internal enum CoinPlacementMode { Simulation, RealUserFolders }

/// <summary>Read-only, constrained lab settings. No command execution or OS shutdown settings.</summary>
internal static class Config
{
    public static int InfectionDuration { get; private set; } = 90;
    public static int RansomAmount { get; private set; } = 500;
    public static bool ChangeWallpaper { get; private set; } = true;
    // Never place files in real user folders unless explicitly opted in.
    public static CoinPlacementMode CoinPlacement { get; private set; } = CoinPlacementMode.Simulation;
    public static string EndMessage { get; private set; } =
        "Vous avez laissé votre ordinateur déverrouillé. Vos fichiers ont été chiffrés et toutes vos données sont perdues.";
    public static string SuccessMessage { get; private set; } =
        "Bravo ! Vous avez récupéré assez de pièces pour sauver votre ordinateur.";

    private sealed class Settings
    {
        public int DurationSeconds { get; set; } = 90;
        public int TargetGold { get; set; } = 500;
        public bool ChangeWallpaper { get; set; } = true;
        public string CoinPlacement { get; set; } = "Simulation";
        public string? FailureMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public static void Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "labsettings.json");
        if (!File.Exists(path)) return;
        try
        {
            Settings? s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (s == null) return;
            InfectionDuration = Math.Clamp(s.DurationSeconds, 15, 3600);
            RansomAmount = Math.Clamp(s.TargetGold, 50, 5000);
            ChangeWallpaper = s.ChangeWallpaper;
            CoinPlacement = string.Equals(s.CoinPlacement, "RealUserFolders", StringComparison.OrdinalIgnoreCase)
                ? CoinPlacementMode.RealUserFolders : CoinPlacementMode.Simulation;
            if (!string.IsNullOrWhiteSpace(s.FailureMessage)) EndMessage = s.FailureMessage[..Math.Min(800, s.FailureMessage.Length)];
            if (!string.IsNullOrWhiteSpace(s.SuccessMessage)) SuccessMessage = s.SuccessMessage[..Math.Min(800, s.SuccessMessage.Length)];
        }
        catch { /* Invalid config: safely use built-in defaults. */ }
    }
}
