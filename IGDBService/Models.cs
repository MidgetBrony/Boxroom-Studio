using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Boxroom_Studio
{
    public class TwitchTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = "";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = "";
    }

    public class IGDBSearchResult
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        public override string ToString()
        {
            if (FirstReleaseDate is null)
                return Name;

            DateTime date = DateTimeOffset
                .FromUnixTimeSeconds(FirstReleaseDate.Value)
                .DateTime;

            return $"{Name} ({date.Year})";
        }
    }

    public class IGDBGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("first_release_date")]
        public long? FirstReleaseDate { get; set; }

        [JsonPropertyName("involved_companies")]
        public List<IGDBInvolvedCompany> InvolvedCompanies { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<IGDBNamedItem> Genres { get; set; } = new();

        [JsonPropertyName("platforms")]
        public List<IGDBNamedItem> Platforms { get; set; } = new();

        [JsonPropertyName("screenshots")]
        public List<IGDBImage> Screenshots { get; set; } = new();

        [JsonPropertyName("cover")]
        public IGDBImage? Cover { get; set; }
    }

    public class IGDBInvolvedCompany
    {
        [JsonPropertyName("company")]
        public IGDBNamedItem? Company { get; set; }

        [JsonPropertyName("developer")]
        public bool Developer { get; set; }

        [JsonPropertyName("publisher")]
        public bool Publisher { get; set; }
    }

    public class IGDBNamedItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class IGDBImage
    {
        [JsonPropertyName("image_id")]
        public string ImageId { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        public string GetUrl(string size = "t_cover_big")
        {
            if (string.IsNullOrWhiteSpace(ImageId))
                return "";

            return $"https://images.igdb.com/igdb/image/upload/{size}/{ImageId}.jpg";
        }
    }
}
