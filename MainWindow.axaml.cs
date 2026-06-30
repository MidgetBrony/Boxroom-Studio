using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Boxroom_Studio
{
    public partial class MainWindow : Window
    {
        public List<CacheGame> CustomGames { get; set; } = new();
        string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
        public Settings Settings { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            bool firstRun = SettingsManager.Load();

            Title = $"Boxroom Studio v{version}";

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


                Debug.WriteLine("CheckForUpdates()");
                CheckForUpdates(true);
            };

            GameEditor.GameSaved += GameEditor_GameSaved;


        }

        public void CheckForUpdates(bool automatic)
        {
            if (automatic && !SettingsManager.Current.AutoUpdate)
                return;

            try
            {
                string updater = Path.Combine(
                    AppContext.BaseDirectory,
                    OperatingSystem.IsWindows()
                        ? "Boxroom-Studio-Updater.exe"
                        : "Boxroom-Studio-Updater");

                if (!File.Exists(updater))
                {
                    Debug.WriteLine("Updater not found.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = updater,
                    Arguments = Process.GetCurrentProcess().Id.ToString(),
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex}");
            }
        }

        private void GameEditor_GameSaved(CacheGame game)
        {
            ListBoxItem? existing = GamesList.Items
                .OfType<ListBoxItem>()
                .FirstOrDefault(i =>
                    i.Tag is CacheGame g &&
                    g.AppId == game.AppId);

            if (existing != null)
            {
                existing.Content = game.Meta.Name;
                return;
            }

            GamesList.Items.Add(new ListBoxItem
            {
                Content = game.Meta.Name,
                Tag = game
            });
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


            CustomGames = await new CacheRespitory().LoadGamesAsync(SettingsManager.Current.CustomOnly);

            GamesList.Items.Clear();

            foreach (CacheGame game in CustomGames)
            {
                GamesList.Items.Add(new ListBoxItem
                {
                    Content = game.Meta.Name,
                    Tag = game
                });
            }

            StatusText.Text = $"Loaded {CustomGames.Count} games.";
        }

        private void GamesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            GameEditor.ShowEditor();
            if (GamesList.SelectedItem is ListBoxItem item &&
                item.Tag is CacheGame game)
            {
                GameEditor.LoadGame(game);
            }
        }

        private void NewGame_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GameEditor.ShowEditor();
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