using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    public class CacheRespitory
    {

        private static int _oldStartingId = 900000000;

        private static int _startingId = -2;

        public int GetNextCustomAppId()
        {
            HashSet<int> usedIds = new();

            if (Directory.Exists(SettingsManager.Current.BoxroomCachePath))
            {
                foreach (string dir in Directory.EnumerateDirectories(SettingsManager.Current.BoxroomCachePath))
                {
                    string folderName = Path.GetFileName(dir);

                    if (int.TryParse(folderName, out int appId))
                    {
                        usedIds.Add(appId);
                    }
                }
            }

            int nextId = _startingId;

            while (usedIds.Contains(nextId))
            {
                nextId--;
            }

            return nextId;
        }
        private static bool UpdateLegacyMeta(SteamMeta meta)
        {
            if (meta == null)
                return false;

            bool changed = false;

            if (!string.IsNullOrWhiteSpace(meta.GameType))
            {
                // Legacy Studio custom entries
                meta.AppType = "custom";
                meta.GameType = "";
                changed = true;
            }

            return changed;
        }

        public async Task<List<CacheGame>> LoadGamesAsync(bool customOnly)
        {
            List<CacheGame> games = new();

            foreach (string dir in Directory.EnumerateDirectories(SettingsManager.Current.BoxroomCachePath))
            {
                Debug.WriteLine(dir);

                try
                {
                    Debug.WriteLine("Reading meta.json");
                    Logger.Info($"Reading meta.json from {dir}");

                    string folderName = Path.GetFileName(dir);

                    if (!int.TryParse(folderName, out int appId))
                    {
                        Debug.WriteLine($"Skipping non-game folder: {folderName}");
                        Logger.Warning($"Skipping non-game folder: {folderName}");
                        continue;
                    }

                    string metaPath = Path.Combine(dir, "meta.json");
                    string backupPath = Path.Combine(dir, "meta.backup.json");

                    // Recover metadata if an older BOXROOM version deleted it.
                    if (!File.Exists(metaPath) && File.Exists(backupPath))
                    {
                        Debug.WriteLine($"Restoring meta.json from backup: {folderName}");
                        Logger.Info($"Restoring meta.json from backup: {folderName}");
                        File.Copy(backupPath, metaPath);
                    }

                    if (!File.Exists(metaPath))
                        continue;

                    SteamMeta? meta;

                    string json = await File.ReadAllTextAsync(metaPath);

                    try
                    {
                        meta = JsonSerializer.Deserialize<SteamMeta>(json);
                    }
                    catch (JsonException ex)
                    {
                        Logger.Warning($"meta.json failed to deserialize for '{folderName}'. Attempting repair...");

                        if (!TryRepairMetaJson(json, out string repairedJson))
                        {
                            Debug.WriteLine($"Invalid meta.json in '{folderName}': {ex.Message}");
                            Logger.Error(ex);
                            continue;
                        }

                        try
                        {
                            meta = JsonSerializer.Deserialize<SteamMeta>(repairedJson);

                            await File.WriteAllTextAsync(metaPath, repairedJson);

                            Logger.Info($"Successfully repaired meta.json for '{folderName}'.");
                        }
                        catch (Exception repairEx)
                        {
                            Debug.WriteLine($"Repair failed for '{folderName}': {repairEx.Message}");
                            Logger.Error(repairEx);
                            continue;
                        }
                    }

                    if (meta == null)
                        continue;

                    bool changed = UpdateLegacyMeta(meta);

                    if (changed)
                    {
                        await File.WriteAllTextAsync(
                            metaPath,
                            JsonSerializer.Serialize(meta, JsonOptions));

                        Logger.Info($"Updated legacy metadata: {meta.Name}");
                    }

                    if (string.IsNullOrWhiteSpace(meta.Name))
                        continue;

                    // -------------------------
                    // Helper
                    // -------------------------

                    MetaHelper helper = new()
                    {
                        Type = "Steam"
                    };

                    string helperPath = Path.Combine(dir, "meta.helper.json");

                    if (File.Exists(helperPath))
                    {
                        try
                        {
                            helper = JsonSerializer.Deserialize<MetaHelper>(
                                await File.ReadAllTextAsync(helperPath))
                                ?? new MetaHelper { Type = "Steam" };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Invalid helper file in '{folderName}': {ex.Message}");
                            Logger.Error(ex);

                            helper = new MetaHelper
                            {
                                Type = "Steam"
                            };
                        }
                    }

                    if (string.Equals(meta.AppType, "custom", StringComparison.OrdinalIgnoreCase))
                    {
                        helper.Type = "Custom";
                    }
                    else if (string.IsNullOrWhiteSpace(helper.Type))
                    {
                        helper.Type = "Steam";
                    }

                    // Official BOXROOM custom games don't have helper files.
                    bool isCustom =
                        helper.Type.Equals("Custom", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(meta.AppType, "custom", StringComparison.OrdinalIgnoreCase);

                    Debug.WriteLine($"Helper: {helper.Type}");
                    Debug.WriteLine($"Custom: {isCustom}");

                    if (customOnly && !isCustom)
                        continue;

                    // -------------------------
                    // Launch
                    // -------------------------

                    LaunchInfo? launch = null;

                    string launchPath = Path.Combine(dir, "launch.json");

                    if (File.Exists(launchPath))
                    {
                        try
                        {
                            launch = JsonSerializer.Deserialize<LaunchInfo>(
                                await File.ReadAllTextAsync(launchPath));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Invalid launch.json in '{folderName}': {ex.Message}");
                            Logger.Error(ex);
                        }
                    }

                    // Official BOXROOM stores launch information directly in meta.json.
                    if (launch == null &&
                        !string.IsNullOrWhiteSpace(meta.LaunchExePath))
                    {
                        launch = new LaunchInfo
                        {
                            Executable = meta.LaunchExePath,
                            WorkingDirectory = Path.GetDirectoryName(meta.LaunchExePath) ?? "",
                            Arguments = "",
                            UseShellExecute = true
                        };
                    }

                    Debug.WriteLine($"Launch: {(launch != null)}");

                    CacheGame game = new()
                    {
                        AppId = appId,
                        Folder = dir,
                        Meta = meta,
                        Helper = helper,
                        Launch = launch
                    };

                    // -------------------------
                    // Cover
                    // -------------------------

                    string boxart = Path.Combine(dir, "boxart.jpg");
                    string cover = Path.Combine(dir, "cover.jpg");

                    if (File.Exists(boxart))
                        game.CoverPath = boxart;
                    else if (File.Exists(cover))
                        game.CoverPath = cover;

                    // -------------------------
                    // Screenshots
                    // -------------------------

                    foreach (string image in Directory.EnumerateFiles(dir, "screen_*.jpg"))
                    {
                        game.Screenshots.Add(image);
                    }

                    Debug.WriteLine($"Adding {meta.Name}");
                    games.Add(game);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading game from directory '{dir}': {ex}");
                    Logger.Error(ex);
                }
            }

            return games;
        }

        private static bool TryRepairMetaJson(string json, out string repairedJson)
        {
            repairedJson = json;

            if (string.IsNullOrEmpty(json))
                return false;

            StringBuilder sb = new(json.Length + 64);

            bool inString = false;
            bool escaped = false;
            bool changed = false;

            foreach (char c in json)
            {
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    sb.Append(c);
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    switch (c)
                    {
                        case '\r':
                            sb.Append("\\r");
                            changed = true;
                            break;

                        case '\n':
                            sb.Append("\\n");
                            changed = true;
                            break;

                        case '\t':
                            sb.Append("\\t");
                            changed = true;
                            break;

                        default:
                            sb.Append(c);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (!changed)
                return false;

            repairedJson = sb.ToString();
            return true;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public async Task SaveGameAsync(CacheGame game)
        {
            if (string.IsNullOrWhiteSpace(game.Folder))
            {
                game.Folder = Path.Combine(
                    SettingsManager.Current.BoxroomCachePath,
                    game.AppId.ToString());
            }

            Directory.CreateDirectory(game.Folder);

            // Keep AppId in sync
            game.Meta.AppId = game.AppId;

            string metaPath = Path.Combine(game.Folder, CacheFiles.Meta);
            string metaBackupPath = Path.Combine(game.Folder, "meta.backup.json");

            // Backup existing metadata before overwriting.
            // Used to recover from older BOXROOM versions that may delete meta.json.
            if (File.Exists(metaPath))
            {
                File.Copy(metaPath, metaBackupPath, overwrite: true);
            }

            if (game.Launch != null)
            {
                game.Meta.LaunchExePath = game.Launch.Executable;
            }
            // meta.json
            await File.WriteAllTextAsync(
                metaPath,
                JsonSerializer.Serialize(game.Meta, JsonOptions));

            // meta.helper.json
            await File.WriteAllTextAsync(
                Path.Combine(game.Folder, CacheFiles.Helper),
                JsonSerializer.Serialize(game.Helper, JsonOptions));

            // launch.json
            string launchPath = Path.Combine(game.Folder, CacheFiles.Launch);

            if (game.Launch != null &&
                !string.IsNullOrWhiteSpace(game.Launch.Executable))
            {
                await File.WriteAllTextAsync(
                    launchPath,
                    JsonSerializer.Serialize(game.Launch, JsonOptions));

                game.Meta.LaunchExePath = game.Launch.Executable;
            }
            else if (File.Exists(launchPath))
            {
                File.Delete(launchPath);
            }

            // boxart.jpg
            if (game.PendingCoverBytes != null)
            {
                string coverPath = Path.Combine(
                    game.Folder,
                    CacheFiles.Cover);

                await File.WriteAllBytesAsync(
                    coverPath,
                    game.PendingCoverBytes);

                game.CoverPath = coverPath;

                game.PendingCoverBytes = null;
                game.PendingCoverBitmap = null;
                game.PendingCoverUrl = null;
            }

            // Remove old screenshots
            foreach (string file in Directory.GetFiles(game.Folder, "screen_*.jpg"))
            {
                File.Delete(file);
            }

            // Write new screenshots
            for (int i = 0; i < game.PendingScreenshotBytes.Count; i++)
            {
                await File.WriteAllBytesAsync(
                    Path.Combine(game.Folder, $"screen_{i}.jpg"),
                    game.PendingScreenshotBytes[i]);
            }

            // Refresh cached file list
            game.Screenshots = Directory
                .GetFiles(game.Folder, "screen_*.jpg")
                .OrderBy(Path.GetFileName)
                .ToList();

            // Clear pending screenshots
            game.PendingScreenshotBytes.Clear();
            game.PendingScreenshotBitmaps.Clear();
        }

        public static async Task SyncCustomGamesAsync()
        {


            string ownedGamesPath = Path.Combine(
                SettingsManager.Current.BoxroomCachePath,
                "owned_games.json");

            if (!File.Exists(ownedGamesPath))
                throw new FileNotFoundException(
                    "Unable to locate owned_games.json.",
                    ownedGamesPath);

            OwnedGames? owned =
                JsonSerializer.Deserialize<OwnedGames>(
                    await File.ReadAllTextAsync(ownedGamesPath));

            if (owned == null)
                throw new InvalidOperationException(
                    "owned_games.json is invalid.");

            // Remove all custom AppIds first
            owned.AppIds.RemoveAll(id => id <= _startingId || id >= _oldStartingId);
            // Ensure collections exist (older files may not contain them)
            owned.Names ??= new Dictionary<int, string>();
            owned.Playtime ??= new Dictionary<int, int>();


            foreach (var id in owned.Names.Keys
     .Where(id => id <= _startingId || id >= _oldStartingId)
     .ToList())
            {
                owned.Names.Remove(id);
                owned.Playtime.Remove(id);
            }
            HashSet<int> existingIds = new(owned.AppIds);

            foreach (string folder in Directory.GetDirectories(
                         SettingsManager.Current.BoxroomCachePath))
            {
                string folderName = Path.GetFileName(folder);

                if (!int.TryParse(folderName, out int appId))
                    continue;

                // Custom AppIds start at _startingId
                if (appId > _startingId)
                    continue;

                string metaPath = Path.Combine(folder, CacheFiles.Meta);

                if (!File.Exists(metaPath))
                    continue;

                SteamMeta? meta;

                try
                {
                    meta = JsonSerializer.Deserialize<SteamMeta>(
                        await File.ReadAllTextAsync(metaPath));

                    if (meta == null || string.IsNullOrWhiteSpace(meta.Name))
                        continue;
                }
                catch
                {
                    // Invalid/corrupt metadata, skip it.
                    continue;
                }

                if (existingIds.Add(appId))
                {
                    owned.AppIds.Add(appId);
                }

                // Keep the dictionaries in sync with AppIds
                owned.Names[appId] = meta.Name;
                owned.Playtime.TryAdd(appId, 0);

            }

            await File.WriteAllTextAsync(
                ownedGamesPath,
                JsonSerializer.Serialize(
                    owned,
                    new JsonSerializerOptions
                    {
                        WriteIndented = false
                    }));


        }

        public async Task DeleteGameAsync(CacheGame game)
        {
            if (string.IsNullOrWhiteSpace(game.Folder))
                return;

            if (Directory.Exists(game.Folder))
            {
                await Task.Run(() => Directory.Delete(game.Folder, true));
            }
        }

        public async Task CheckLegacyAsync()
        {

            foreach (string dir in Directory.EnumerateDirectories(SettingsManager.Current.BoxroomCachePath))
            {
                string folderName = Path.GetFileName(dir);

                if (!int.TryParse(folderName, out int oldId))
                    continue;

                if (oldId < _oldStartingId)
                    continue;

                int newId = GetNextCustomAppId();


                string oldFolder = dir;
                string newFolder = Path.Combine(
                    SettingsManager.Current.BoxroomCachePath,
                    newId.ToString());

                if (Directory.Exists(newFolder))
                {
                    Logger.Warning($"Legacy migration skipped. '{newFolder}' already exists.");
                    continue;
                }

                Directory.Move(oldFolder, newFolder);

                string metaPath = Path.Combine(newFolder, CacheFiles.Meta);

                if (File.Exists(metaPath))
                {
                    SteamMeta? meta =
                        JsonSerializer.Deserialize<SteamMeta>(
                            await File.ReadAllTextAsync(metaPath));

                    if (meta != null)
                    {
                        meta.AppId = newId;
                        meta.AppType = "custom";
                        meta.GameType = "";

                        await File.WriteAllTextAsync(
                            metaPath,
                            JsonSerializer.Serialize(meta, JsonOptions));
                    }
                }
            }

            await SyncCustomGamesAsync();
        }
    }
}
