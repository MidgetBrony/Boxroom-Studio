using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    public partial class MainWindow : Window
    {
        public List<CacheGame> CustomGames { get; set; } = new();

        public Settings Settings { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            bool firstRun = SettingsManager.Load();

            Settings = SettingsManager.Current;

            ThemeManager.Apply(Settings.Theme);

            Opened += async (_, _) =>
            {
                if (firstRun)
                {
                    await new SettingsForm(true).ShowDialog(this);

                    // User may have changed the theme.
                    ThemeManager.Apply(SettingsManager.Current.Theme);
                }

                Debug.WriteLine("LoadCustomGames()");
                await LoadCustomGames();
            };
        }

        private async Task LoadCustomGames()
        {
            StatusText.Text = "Loading custom games...";

            string cachePath = SettingsManager.Current.BoxroomCachePath;

            if (!Directory.Exists(cachePath))
            {
                Debug.WriteLine($"Cache path does not exist: {cachePath}");

                StatusText.Text = "BOXROOM cache folder not found.";

                return;
            }

            CustomGames = await new CacheRespitory().LoadGamesAsync(true);

            Debug.WriteLine($"Loaded {CustomGames.Count} custom games.");

            GamesList.Items.Clear();

            foreach (CacheGame game in CustomGames)
            {
                GamesList.Items.Add(new ListBoxItem
                {
                    Content = game.Meta.Name,
                    Tag = game
                });
            }

            StatusText.Text = $"Loaded {CustomGames.Count} custom games.";
        }

        private void GamesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GamesList.SelectedItem is ListBoxItem item &&
                item.Tag is CacheGame game)
            {
                GameEditor.LoadGame(game);
            }
        }

        private void NewGame_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GameEditor.NewGame();
        }

        private async void SyncGamesOwned(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                SyncOwnedGamesButton.IsEnabled = false;

                StatusText.Text = "Synchronizing custom games...";

                await CacheRespitory.SyncCustomGamesAsync();

                StatusText.Text = "Custom games synchronized.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Synchronization failed: {ex.Message}";
            }
            finally
            {
                SyncOwnedGamesButton.IsEnabled = true;
            }
        }

        private async void OpenSettings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SettingsForm settings = new();

            await settings.ShowDialog(this);

            // Apply any theme change immediately.
            ThemeManager.Apply(SettingsManager.Current.Theme);
        }
    }
}