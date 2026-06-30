using Avalonia;
using Avalonia.Styling;
using System;
using System.IO;
using System.Text.Json;

namespace Boxroom_Studio
{
    public static class ThemeManager
    {
        public static void Apply(string theme)
        {
            if (Application.Current == null)
                return;

            Application.Current.RequestedThemeVariant =
                theme switch
                {
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
        }
    }

    public class Settings
    {
        public string BoxroomCachePath { get; set; } = "";
        public string IGDBClientId { get; set; } = "";
        public string IGDBClientSecret { get; set; } = "";
        public string IGDBToken { get; set; } = "";
        public int IGDBTokenExpiresIn { get; set; }
        public string SteamGridDBApiKey { get; set; } = "";
        public string Theme { get; set; } = "Dark";
        public bool CustomOnly { get; set; } = false;
        public bool AutoUpdate { get; set; } = true;
    }

    public static class SettingsManager
    {
        private const string FileName = "settings.json";

        public static Settings Current { get; private set; } = new();

        public static string GetDefaultCachePath()
        {
            if (OperatingSystem.IsWindows())
            {
                return Path.GetFullPath(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "..",
                        "LocalLow",
                        "NestedLoop",
                        "BOXROOM",
                        "steam_cache_v2"));
            }

            // TODO: Linux default path
            return "";
        }

        /// <summary>
        /// Loads settings.
        /// Returns true if this is the first launch.
        /// </summary>
        public static bool Load()
        {
            bool firstRun = !File.Exists(FileName);

            if (firstRun)
            {
                Current = new Settings
                {
                    BoxroomCachePath = GetDefaultCachePath()
                };

                Save();

                return true;
            }

            Current = JsonSerializer.Deserialize<Settings>(
                File.ReadAllText(FileName)) ?? new Settings();

            if (string.IsNullOrWhiteSpace(Current.BoxroomCachePath))
            {
                Current.BoxroomCachePath = GetDefaultCachePath();
            }

            Save();
            return false;
        }

        public static void Save()
        {
            File.WriteAllText(
                FileName,
                JsonSerializer.Serialize(Current, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
        }
    }
}