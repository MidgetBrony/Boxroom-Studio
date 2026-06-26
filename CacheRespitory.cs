using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    public class CacheRespitory
    {

        public int GetNextCustomAppId()
        {
            const int StartingId = 900000000;

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

            int nextId = StartingId;

            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId;
        }

        public async Task<List<CacheGame>> LoadGamesAsync(bool customOnly)
        {
            List<CacheGame> games = new();

            foreach (string dir in Directory.EnumerateDirectories(SettingsManager.Current.BoxroomCachePath))
            {
                string metaPath = Path.Combine(dir, "meta.json");
                if (!File.Exists(metaPath))
                    continue;

                SteamMeta? meta = JsonSerializer.Deserialize<SteamMeta>(
                    await File.ReadAllTextAsync(metaPath));

                if (meta == null)
                    continue;

                MetaHelper? helper = null;
                string helperPath = Path.Combine(dir, "meta.helper.json");

                if (File.Exists(helperPath))
                {
                    helper = JsonSerializer.Deserialize<MetaHelper>(
                        await File.ReadAllTextAsync(helperPath));
                }

                if (customOnly && helper?.Type != "Custom")
                    continue;

                LaunchInfo? launch = null;
                string launchPath = Path.Combine(dir, "launch.json");

                if (File.Exists(launchPath))
                {
                    launch = JsonSerializer.Deserialize<LaunchInfo>(
                        await File.ReadAllTextAsync(launchPath));
                }

                CacheGame game = new()
                {
                    AppId = int.Parse(Path.GetFileName(dir)),
                    Folder = dir,
                    Meta = meta,
                    Helper = helper,
                    Launch = launch
                };

                // Cover
                string boxart = Path.Combine(dir, "boxart.jpg");
                string cover = Path.Combine(dir, "cover.jpg");

                if (File.Exists(boxart))
                    game.CoverPath = boxart;
                else if (File.Exists(cover))
                    game.CoverPath = cover;

                // Screenshots
                foreach (string image in Directory.EnumerateFiles(dir, "screen_*.jpg"))
                {
                    game.Screenshots.Add(image);
                }

                games.Add(game);
            }

            return games;
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

            // meta.json
            await File.WriteAllTextAsync(
                Path.Combine(game.Folder, CacheFiles.Meta),
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

            HashSet<int> existingIds = new(owned.AppIds);

            foreach (string folder in Directory.GetDirectories(
                         SettingsManager.Current.BoxroomCachePath))
            {
                string folderName = Path.GetFileName(folder);

                if (!int.TryParse(folderName, out int appId))
                    continue;

                // Custom AppIds start at 900000000
                if (appId < 900000000)
                    continue;

                if (existingIds.Add(appId))
                {
                    owned.AppIds.Add(appId);
                }
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
    }
}
