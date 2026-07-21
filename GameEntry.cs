using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace Boxroom_Studio
{
    public static class CacheFiles
    {
        public const string Meta = "meta.json";
        public const string Helper = "meta.helper.json";
        public const string Launch = "launch.json";
        public const string Cover = "boxart.jpg";
    }

    public class OwnedGames
    {
        public List<int> AppIds { get; set; } = new();

        public Dictionary<int, string> Names { get; set; } = new();

        public Dictionary<int, int> Playtime { get; set; } = new();
    }

    public class SteamMeta
    {
        public int PlayTimeMinutes { get; set; }

        public string? AppType { get; set; }

        // Legacy - migrate then remove later
        public string? GameType { get; set; }

        public string Name { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string DetailedDescription { get; set; } = "";
        public string AboutTheGame { get; set; } = "";

        public string BoxArtUrlBase { get; set; } = "";
        public string FallbackHeaderUrl { get; set; } = "";
        public string ReleaseDate { get; set; } = "";

        public List<string> Developers { get; set; } = new();
        public List<string> Publishers { get; set; } = new();
        public List<string> Genres { get; set; } = new();

        public string LaunchExePath { get; set; } = "";

        public List<string> ScreenshotUrls { get; set; } = new();

        // Legacy only
        public int AppId { get; set; } = -1;
    }

    public class CacheGame
    {
        public string Folder { get; set; } = "";

        public int AppId { get; set; }

        public int? IGDBId { get; set; }

        public SteamMeta Meta { get; set; } = new();

        public MetaHelper Helper { get; set; } = new();

        public LaunchInfo? Launch { get; set; }

        public string? CoverPath { get; set; }

        public List<string> Screenshots { get; set; } = new();


        public string? PendingCoverUrl { get; set; }

        public byte[]? PendingCoverBytes { get; set; }

        public Bitmap? PendingCoverBitmap { get; set; }

        public List<byte[]> PendingScreenshotBytes { get; } = new();

        public List<Bitmap> PendingScreenshotBitmaps { get; } = new();
    }

    public class MetaHelper
    {
        /// <summary>
        /// Entry type used by Boxroom Studio.
        /// Examples: Custom, Steam
        /// </summary>
        public string Type { get; set; } = "Custom";

        /// <summary>
        /// Platform used by Boxroom-Plus.
        /// Examples: steam, gog, epic, ea, ubisoft, itch, emulator, rom, custom
        /// </summary>
        public string Platform { get; set; } = "custom";

        /// <summary>
        /// Optional override for the case color.
        /// HTML color (#RRGGBB or #RRGGBBAA).
        /// Empty = use platform default.
        /// </summary>
        public string CaseColor { get; set; } = "";

        public int? IGDBId { get; set; }

        public int? SteamGridDBId { get; set; }
    }

    public class LaunchInfo
    {
        public string Executable { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public string Arguments { get; set; } = "";
        public bool UseShellExecute { get; set; } = true;
    }
}