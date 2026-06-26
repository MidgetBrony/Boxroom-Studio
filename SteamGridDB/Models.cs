using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Boxroom_Studio
{
    /// <summary>
    /// Generic SteamGridDB API response.
    /// </summary>
    public class SteamGridResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    /// <summary>
    /// SteamGridDB Game
    /// </summary>
    public class SteamGridGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// SteamGridDB Grid (Cover)
    /// </summary>
    public class SteamGridImage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("style")]
        public string Style { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("mime")]
        public string Mime { get; set; } = "";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("thumb")]
        public string Thumb { get; set; } = "";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("author")]
        public SteamGridAuthor? Author { get; set; }
    }

    public class SteamGridAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("steam64")]
        public string Steam64 { get; set; } = "";

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; } = "";
    }

}
