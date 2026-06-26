using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    public class IGDBService
    {
        private const string TokenUrl = "https://id.twitch.tv/oauth2/token";
        private const string GamesUrl = "https://api.igdb.com/v4/games";

        private readonly HttpClient _client = new();

        public async Task<string> GetAccessTokenAsync()
        {
            Debug.WriteLine("Getting Token");

            if (string.IsNullOrWhiteSpace(SettingsManager.Current.IGDBClientId))
                throw new InvalidOperationException("IGDB Client ID is missing.");

            if (string.IsNullOrWhiteSpace(SettingsManager.Current.IGDBClientSecret))
                throw new InvalidOperationException("IGDB Client Secret is missing.");

            string url =
                $"{TokenUrl}" +
                $"?client_id={SettingsManager.Current.IGDBClientId}" +
                $"&client_secret={SettingsManager.Current.IGDBClientSecret}" +
                $"&grant_type=client_credentials";

            using HttpResponseMessage response = await _client.PostAsync(url, null);

            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(json);

            TwitchTokenResponse? token =
                JsonSerializer.Deserialize<TwitchTokenResponse>(json);

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new InvalidOperationException("IGDB token response was empty.");

            SettingsManager.Current.IGDBToken = token.AccessToken;
            SettingsManager.Current.IGDBTokenExpiresIn = token.ExpiresIn;
            SettingsManager.Save();

            return token.AccessToken;
        }

        private async Task EnsureTokenAsync()
        {
            Debug.WriteLine("EnsureTokenAsync");
            Debug.WriteLine($"Token = '{SettingsManager.Current.IGDBToken}'");

            if (string.IsNullOrWhiteSpace(SettingsManager.Current.IGDBToken))
                await GetAccessTokenAsync();
        }

        public async Task<List<IGDBSearchResult>> SearchGamesAsync(string text)
        {
            await EnsureTokenAsync();

            string query = $"""
        search "{EscapeSearch(text)}";
        fields id,name,first_release_date;
        limit 50;
        """;

            string json = await PostIgdbAsync(query);

            return JsonSerializer.Deserialize<List<IGDBSearchResult>>(json) ?? new();
        }

        public async Task<IGDBGame?> LoadGameDetailsAsync(int id)
        {
            await EnsureTokenAsync();

            string query = $"""
        fields
            name,
            summary,
            first_release_date,
            involved_companies.company.name,
            involved_companies.developer,
            involved_companies.publisher,
            genres.name,
            platforms.name,
            screenshots.image_id,
            cover.image_id;

        where id = {id};
        """;

            string json = await PostIgdbAsync(query);

            return JsonSerializer.Deserialize<List<IGDBGame>>(json)?.FirstOrDefault();
        }

        private async Task<string> PostIgdbAsync(string query)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, GamesUrl);

            request.Headers.Add("Client-ID", SettingsManager.Current.IGDBClientId);
            request.Headers.Add("Authorization", $"Bearer {SettingsManager.Current.IGDBToken}");

            request.Content = new StringContent(query, Encoding.UTF8, "text/plain");

            using HttpResponseMessage response = await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();

                // Token may have expired; retry once.
                if ((int)response.StatusCode == 401)
                {
                    SettingsManager.Current.IGDBToken = "";
                    SettingsManager.Save();

                    await GetAccessTokenAsync();

                    return await PostIgdbAsync(query);
                }

                throw new InvalidOperationException($"IGDB request failed: {error}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        private static string EscapeSearch(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
