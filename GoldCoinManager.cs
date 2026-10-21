using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace rans0m;

internal static class CoinValues
{
    public static readonly Dictionary<int, int> ExtensionValues = new()
    {
        { 1, 10 }, { 2, 50 }, { 3, 100 }, { 4, 150 }, { 5, 200 }
    };
    public static int WeightedType() => Global.rng.Next(100) switch
    {
        < 40 => 1, < 70 => 2, < 85 => 3, < 93 => 4, _ => 5
    };
}

/// <summary>
/// The manifest lives exclusively in an isolated per-run LocalAppData session.
/// With explicit opt-in, generated coin images may also be placed directly in six
/// approved current-user folders. Never reads/deletes unrelated personal files,
/// edits registry file associations, traverses personal trees, or removes folders
/// from those approved real locations.
/// </summary>
internal static class GoldCoinManager
{
    private sealed class CoinRecord
    {
        public string File { get; set; } = "";
        public string Token { get; set; } = "";
        public int Value { get; set; }
        // Only PNG coins use hashes; simulated .gold* files contain their own token.
        public string Sha256 { get; set; } = "";
    }

    private sealed class Manifest
    {
        public string Session { get; set; } = "";
        public string Placement { get; set; } = nameof(CoinPlacementMode.Simulation);
        public List<CoinRecord> Coins { get; set; } = new();
        public List<string> Directories { get; set; } = new();
    }

    private static readonly object Gate = new();
    private static readonly string SessionParent = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rans0mAwarenessLab", "Sessions");
    private static readonly Dictionary<string, CoinRecord> Coins = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> OwnedDirectories = new();
    private static readonly string[] FolderNames =
    {
        "Bureau (simulation)", "Documents (simulation)", "Téléchargements (simulation)",
        "Images (simulation)", "Musique (simulation)", "Vidéos (simulation)"
    };
    private static string? SessionId;
    private static CoinPlacementMode ActivePlacement;
    public static string? WorkspaceRoot { get; private set; }
    private static string? ManifestPath => WorkspaceRoot == null ? null : Path.Combine(WorkspaceRoot, "manifest.json");

    public static IReadOnlyList<string> ListActiveCoins()
    {
        lock (Gate) return Coins.Keys.Where(File.Exists).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static int GenerateCoins(byte[]? imageBytes = null)
    {
        lock (Gate)
        {
            if (WorkspaceRoot != null) throw new InvalidOperationException("A lab session is already active");
            ActivePlacement = Config.CoinPlacement;
            var dirs = ActivePlacement == CoinPlacementMode.RealUserFolders
                ? ApprovedRealFolders()
                : new List<string>();
            if (ActivePlacement == CoinPlacementMode.RealUserFolders && (dirs.Count == 0 || imageBytes == null || imageBytes.Length == 0))
                throw new InvalidOperationException("Aucun dossier utilisateur local admissible, ou image de pièce absente.");

            Directory.CreateDirectory(SessionParent);
            SessionId = Guid.NewGuid().ToString("N");
            WorkspaceRoot = Path.Combine(SessionParent, SessionId);
            Directory.CreateDirectory(WorkspaceRoot);
            using (var marker = new FileStream(Path.Combine(WorkspaceRoot, ".lab-owned"), FileMode.CreateNew)) { }
            PersistManifest();

            try
            {
                if (ActivePlacement == CoinPlacementMode.Simulation)
                {
                    foreach (string name in FolderNames)
                    {
                        string parent = Path.Combine(WorkspaceRoot, name);
                        Directory.CreateDirectory(parent);
                        OwnedDirectories.Add(parent);
                        PersistManifest();
                        dirs.Add(parent);
                        string deeper = Path.Combine(parent, "Dossier " + Global.rng.Next(100, 999));
                        Directory.CreateDirectory(deeper);
                        OwnedDirectories.Add(deeper);
                        PersistManifest();
                        dirs.Add(deeper);
                    }
                }

                string imageHash = imageBytes == null ? "" : Convert.ToHexString(SHA256.HashData(imageBytes));
                int gold = 0;
                int needed = (int)Math.Ceiling(Config.RansomAmount * 1.35);
                int attempts = 0;
                while (gold < needed && ++attempts <= 500)
                {
                    int type = CoinValues.WeightedType();
                    int value = CoinValues.ExtensionValues[type];
                    string directory = dirs[Global.rng.Next(dirs.Count)];
                    string token = Guid.NewGuid().ToString("N");
                    string file = ActivePlacement == CoinPlacementMode.RealUserFolders
                        ? Path.Combine(directory, $"RANS0M-Lab-Coin-{token}-G{value}.png")
                        : Path.Combine(directory, token + ".gold" + type);
                    var record = new CoinRecord
                    {
                        File = file, Token = token, Value = value,
                        Sha256 = ActivePlacement == CoinPlacementMode.RealUserFolders ? imageHash : ""
                    };

                    // Persist the intent before creating the file: a crash after creation
                    // cannot strand an untracked coin. CreateNew never overwrites files.
                    Coins[file] = record;
                    PersistManifest();
                    bool created = false;
                    try
                    {
                        using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                        {
                            created = true;
                            if (ActivePlacement == CoinPlacementMode.RealUserFolders)
                                stream.Write(imageBytes!);
                            else
                                JsonSerializer.Serialize(stream, new { Session = SessionId, Token = token, Value = value });
                        }
                        gold += value;
                    }
                    catch (IOException)
                    {
                        // This process was the exclusive creator, and the write failed.
                        // Leave no partially written PNG behind on recoverable errors.
                        if (created) { try { File.Delete(file); } catch { } }
                        Coins.Remove(file);
                        PersistManifest();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (created) { try { File.Delete(file); } catch { } }
                        Coins.Remove(file);
                        PersistManifest();
                    }
                }
                if (gold < Config.RansomAmount)
                    throw new IOException("Impossible de générer assez de pièces dans les dossiers autorisés.");
                return gold;
            }
            catch
            {
                // Roll back only cryptographically validated coins created by this run.
                DeleteAllCoins();
                throw;
            }
        }
    }

    private static void PersistManifest()
    {
        if (ManifestPath == null) return;
        string temp = ManifestPath + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
            JsonSerializer.Serialize(stream, new Manifest
            {
                Session = SessionId ?? "",
                Placement = ActivePlacement.ToString(),
                Coins = Coins.Values.ToList(),
                Directories = OwnedDirectories.ToList()
            });
        File.Move(temp, ManifestPath, true);
    }

    public static bool TryRedeem(string file, out int amount)
    {
        lock (Gate)
        {
            amount = 0;
            string path;
            try { path = Path.GetFullPath(file); }
            catch { return false; }
            if (WorkspaceRoot == null || !Coins.TryGetValue(path, out CoinRecord? coin)) return false;
            if (!IsPermittedCoinPath(coin, WorkspaceRoot, ActivePlacement) || !IsOurCoin(coin, SessionId, ActivePlacement))
                return false;
            try { File.Delete(path); }
            catch { return false; }
            amount = coin.Value;
            Coins.Remove(path);
            PersistManifest();
            return true;
        }
    }

    private static bool IsOurCoin(CoinRecord coin, string? session, CoinPlacementMode placement)
    {
        try
        {
            if ((File.GetAttributes(coin.File) & FileAttributes.ReparsePoint) != 0) return false;
            if (placement == CoinPlacementMode.RealUserFolders)
            {
                // A PNG is considered ours only if its unique name AND every byte still match.
                string expected = $"RANS0M-Lab-Coin-{coin.Token}-G{coin.Value}.png";
                return string.Equals(Path.GetFileName(coin.File), expected, StringComparison.OrdinalIgnoreCase)
                    && coin.Sha256.Length == 64
                    && string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(coin.File))),
                        coin.Sha256, StringComparison.OrdinalIgnoreCase);
            }
            if (!CoinValues.ExtensionValues.Any(kv => kv.Value == coin.Value
                    && coin.File.EndsWith(".gold" + kv.Key, StringComparison.OrdinalIgnoreCase))) return false;
            using var document = JsonDocument.Parse(File.ReadAllText(coin.File));
            return document.RootElement.GetProperty("Session").GetString() == session
                && document.RootElement.GetProperty("Token").GetString() == coin.Token
                && document.RootElement.GetProperty("Value").GetInt32() == coin.Value;
        }
        catch { return false; }
    }

    private static bool Inside(string file, string root)
    {
        string relative = Path.GetRelativePath(root, file);
        return relative != "." && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar)
            && !Path.IsPathRooted(relative);
    }

    private static bool IsPermittedCoinPath(CoinRecord coin, string sessionRoot, CoinPlacementMode placement)
    {
        try
        {
            string path = Path.GetFullPath(coin.File);
            if (placement == CoinPlacementMode.Simulation)
                return Inside(path, Path.GetFullPath(sessionRoot)) && !HasReparseComponent(Path.GetDirectoryName(path)!);
            // Real-folder mode does not descend into existing user subdirectories:
            // the exact immediate parent must be one of the approved known folders.
            string parent = Path.GetDirectoryName(path)!;
            return ApprovedRealFolders().Any(dir => string.Equals(parent, dir, StringComparison.OrdinalIgnoreCase))
                && !HasReparseComponent(parent);
        }
        catch { return false; }
    }

    private static List<string> ApprovedRealFolders()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(home, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
        };
        var cloudRoots = new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" }
            .Select(Environment.GetEnvironmentVariable)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(path!))
            .ToArray();
        var approved = new List<string>();
        foreach (string candidate in candidates)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidate) || !Directory.Exists(candidate)) continue;
                string dir = Path.GetFullPath(candidate);
                if (!Inside(dir, Path.GetFullPath(home)) || HasReparseComponent(dir)) continue;
                // Avoid copying lab coins into a synchronized personal cloud folder.
                if (cloudRoots.Any(root => string.Equals(dir, root, StringComparison.OrdinalIgnoreCase) || Inside(dir, root))) continue;
                if (!approved.Contains(dir, StringComparer.OrdinalIgnoreCase)) approved.Add(dir);
            }
            catch { /* This known folder is unavailable or protected. */ }
        }
        return approved;
    }

    private static bool HasReparseComponent(string path)
    {
        try
        {
            DirectoryInfo? directory = new(Path.GetFullPath(path));
            while (directory != null)
            {
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) return true;
                directory = directory.Parent;
            }
            return false;
        }
        catch { return true; }
    }

    public static void DeleteAllCoins()
    {
        lock (Gate)
        {
            if (WorkspaceRoot == null) return;
            CleanSession(WorkspaceRoot);
            Coins.Clear(); OwnedDirectories.Clear(); WorkspaceRoot = null; SessionId = null;
        }
    }

    public static void RecoverOrphanedSessions()
    {
        try
        {
            if (!Directory.Exists(SessionParent)) return;
            foreach (string dir in Directory.EnumerateDirectories(SessionParent)) CleanSession(dir);
        }
        catch { }
    }

    private static void CleanSession(string dir)
    {
        try
        {
            if (!Inside(Path.GetFullPath(dir), Path.GetFullPath(SessionParent)) || HasReparseComponent(dir)
                || !File.Exists(Path.Combine(dir, ".lab-owned"))) return;
            string manifestFile = Path.Combine(dir, "manifest.json");
            Manifest? manifest = File.Exists(manifestFile)
                ? JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestFile)) : null;
            if (manifest?.Session != Path.GetFileName(dir)) return;
            if (!Enum.TryParse(manifest.Placement, true, out CoinPlacementMode placement)) return;

            foreach (CoinRecord coin in manifest.Coins)
            {
                if (IsPermittedCoinPath(coin, dir, placement) && IsOurCoin(coin, manifest.Session, placement))
                { try { File.Delete(coin.File); } catch { } }
            }
            // No folder from an actual user location can ever be removed.
            if (placement == CoinPlacementMode.Simulation)
            {
                foreach (string child in manifest.Directories.OrderByDescending(x => x.Length))
                {
                    try
                    {
                        if (!Inside(Path.GetFullPath(child), Path.GetFullPath(dir)) || HasReparseComponent(child)) continue;
                        Directory.Delete(child, false); // fails safely if unknown files remain
                    }
                    catch { }
                }
            }
            // Keep metadata when anything remains, so recovery can retry on next run.
            if (Directory.EnumerateDirectories(dir).Any()) return;
            if (Directory.EnumerateFiles(dir).Any(f => f != manifestFile && f != Path.Combine(dir, ".lab-owned") && f != manifestFile + ".tmp"))
                return;
            // If any coin could not be removed, preserve the manifest (never forget it).
            if (manifest.Coins.Any(c => File.Exists(c.File) && IsPermittedCoinPath(c, dir, placement))) return;
            try { File.Delete(manifestFile + ".tmp"); } catch { }
            File.Delete(manifestFile);
            File.Delete(Path.Combine(dir, ".lab-owned"));
            Directory.Delete(dir, false);
        }
        catch { }
    }
}
