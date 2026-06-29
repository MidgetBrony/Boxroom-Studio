using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.Linq;

namespace Boxroom_Studio;

public partial class SettingsForm : Window
{
    public SettingsForm(bool firstRun = false)
    {
        InitializeComponent();

        FirstRunInfoPanel.IsVisible = firstRun;

        LoadSettings();
    }

    private void LoadSettings()
    {
        CachePathBox.Text = SettingsManager.Current.BoxroomCachePath;

        IGDBClientIdBox.Text = SettingsManager.Current.IGDBClientId;
        IGDBClientSecretBox.Text = SettingsManager.Current.IGDBClientSecret;

        SteamGridApiKeyBox.Text = SettingsManager.Current.SteamGridDBApiKey;

        ThemeBox.SelectedIndex =
            SettingsManager.Current.Theme == "Light" ? 1 : 0;

        IGDBStatusText.Text = "Not tested.";
        IGDBStatusText.Foreground = Brushes.Gray;

        SteamGridStatusText.Text = "Not tested.";
        SteamGridStatusText.Foreground = Brushes.Gray;

        AutoUpdateBox.IsChecked = SettingsManager.Current.AutoUpdate;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        SettingsManager.Current.BoxroomCachePath =
            CachePathBox.Text?.Trim() ?? "";

        SettingsManager.Current.IGDBClientId =
            IGDBClientIdBox.Text?.Trim() ?? "";

        SettingsManager.Current.IGDBClientSecret =
            IGDBClientSecretBox.Text?.Trim() ?? "";

        SettingsManager.Current.SteamGridDBApiKey =
            SteamGridApiKeyBox.Text?.Trim() ?? "";

        SettingsManager.Current.Theme =
            ThemeBox.SelectedIndex == 1 ? "Light" : "Dark";

        SettingsManager.Current.AutoUpdate = AutoUpdateBox.IsChecked ?? true;
        SettingsManager.Save();

        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void BrowseCacheButton_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select BOXROOM steam_cache_v2 Folder",
                AllowMultiple = false
            });

        string? path = folders.FirstOrDefault()?.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            CachePathBox.Text = path;
        }
    }

    private void IGDBHelpButton_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://dev.twitch.tv/console/apps",
            UseShellExecute = true
        });
    }

    private void SteamGridHelpButton_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.steamgriddb.com/profile/preferences/api",
            UseShellExecute = true
        });
    }

    private async void TestIGDBButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            SettingsManager.Current.IGDBClientId =
                IGDBClientIdBox.Text?.Trim() ?? "";

            SettingsManager.Current.IGDBClientSecret =
                IGDBClientSecretBox.Text?.Trim() ?? "";

            await new IGDBService().GetAccessTokenAsync();

            IGDBStatusText.Foreground = Brushes.LimeGreen;
            IGDBStatusText.Text = "✓ Connection successful.";
        }
        catch (Exception ex)
        {
            IGDBStatusText.Foreground = Brushes.IndianRed;
            IGDBStatusText.Text = $"✗ {ex.Message}";
        }
    }

    private async void TestSteamGridButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            SettingsManager.Current.SteamGridDBApiKey =
                SteamGridApiKeyBox.Text?.Trim() ?? "";

            await new SteamGridDBService().SearchGamesAsync("Portal");

            SteamGridStatusText.Foreground = Brushes.LimeGreen;
            SteamGridStatusText.Text = "✓ Connection successful.";
        }
        catch (Exception ex)
        {
            SteamGridStatusText.Foreground = Brushes.IndianRed;
            SteamGridStatusText.Text = $"✗ {ex.Message}";
        }
    }

    private void CheckForUpdatesButton_Click(object? sender, RoutedEventArgs e)
    {
        if (Owner is MainWindow window)
        {
            window.CheckForUpdates(false);
        }
    }
}