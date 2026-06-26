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
    }

    public class SteamMeta
    {
        public int PlayTimeMinutes { get; set; }
        public string Name { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string Type { get; set; } = "";
        public string DetailedDescription { get; set; } = "";
        public string AboutTheGame { get; set; } = "";
        public string BoxArtUrlBase { get; set; } = "";
        public string FallbackHeaderUrl { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public List<string> Developers { get; set; } = new();
        public List<string> Publishers { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public List<string> ScreenshotUrls { get; set; } = new();

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
        public string Type { get; set; } = "Custom";

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