using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    internal class SteamGridDBService
    {
        private const string BaseUrl = "https://www.steamgriddb.com/api/v2";

        private readonly HttpClient _client = new();

        public SteamGridDBService()
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Current.SteamGridDBApiKey))
                throw new InvalidOperationException("SteamGridDB API Key is missing.");

            _client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {SettingsManager.Current.SteamGridDBApiKey}");
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("Boxroom Studio/1.0");
        }

        /// <summary>
        /// Resolve an IGDB ID into a SteamGridDB Game.
        /// </summary>
        public async Task<List<SteamGridGame>> SearchGamesAsync(string name)
        {
            string json = await _client.GetStringAsync(
                $"{BaseUrl}/search/autocomplete/{Uri.EscapeDataString(name)}");

            var result = JsonSerializer.Deserialize<
                SteamGridResponse<List<SteamGridGame>>>(json);

            return result?.Data ?? new();
        }

        /// <summary>
        /// Gets available vertical cover art.
        /// </summary>
        public async Task<List<SteamGridImage>> GetGridsAsync(
            int gameId,
            string dimensions = "600x900",
            string styles = "",
            int limit = 50,
            int page = 0)
        {
            StringBuilder url = new();

            url.Append($"{BaseUrl}/grids/game/{gameId}");
            url.Append($"?dimensions={Uri.EscapeDataString(dimensions)}");
            url.Append($"&limit={limit}");
            url.Append($"&page={page}");

            if (!string.IsNullOrWhiteSpace(styles))
                url.Append($"&styles={Uri.EscapeDataString(styles)}");

            string json = await _client.GetStringAsync(url.ToString());

            var result = JsonSerializer.Deserialize<
                SteamGridResponse<List<SteamGridImage>>>(json);

            return result?.Data ?? new();
        }
    }
}