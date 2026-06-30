using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Boxroom_Studio;

public partial class GameEditor : UserControl
{
    private CacheGame? _currentGame;
    public static event Action<CacheGame>? GameSaved;
    public GameEditor()
    {
        InitializeComponent();
    }

    public void ShowEditor()
    {
        EmptyState.IsVisible = false;
        EditorRoot.IsVisible = true;
    }

    public void ShowEmpty()
    {
        EmptyState.IsVisible = true;
        EditorRoot.IsVisible = false;
    }
    public void LoadGame(CacheGame game)
    {
        _currentGame = game;

        // meta.json
        NameBox.Text = game.Meta.Name;
        DescriptionBox.Text = game.Meta.ShortDescription;

        // launch.json
        if (game.Launch != null)
        {
            ExecutableBox.Text = game.Launch.Executable;
            ArgumentsBox.Text = game.Launch.Arguments;
            WorkingDirectoryBox.Text = game.Launch.WorkingDirectory;
            UseShellExecuteBox.IsChecked = game.Launch.UseShellExecute;
        }
        else
        {
            ExecutableBox.Text = "";
            ArgumentsBox.Text = "";
            WorkingDirectoryBox.Text = "";
            UseShellExecuteBox.IsChecked = true;

        }
        _currentGame.PendingCoverBytes = null;
        _currentGame.PendingCoverBitmap = null;
        _currentGame.PendingCoverUrl = null;
        // cover
        if (!string.IsNullOrWhiteSpace(game.CoverPath))
        {
            CoverImage.Source = new Avalonia.Media.Imaging.Bitmap(game.CoverPath);
        }
        else
        {
            CoverImage.Source = null;
        }

        ReleaseDatePicker.SelectedDate = Convert.ToDateTime(game.Meta.ReleaseDate);
        DeveloperBox.Text = string.Join(", ", game.Meta.Developers);
        PublisherBox.Text = string.Join(", ", game.Meta.Publishers);
        GenreBox.Text = string.Join(", ", game.Meta.Genres);

        // screenshots
        ScreenshotsList.Items.Clear();

        EditorTitle.Text = $"Cache ID: {game.AppId}";

        foreach (string screenshot in game.Screenshots)
        {
            ScreenshotsList.Items.Add(screenshot);
        }


    }

    public void NewGame()
    {
        _currentGame = new CacheGame
        {
            AppId = new CacheRespitory().GetNextCustomAppId(),
            Meta = new SteamMeta
            {
                Type = "Custom",
                Name = "",
                PlayTimeMinutes = 0
            },
            Launch = new LaunchInfo(),
            Helper = new MetaHelper
            {
                Type = "Custom"
            }
        };



        EditorTitle.Text = $"Cache ID: {_currentGame.AppId}";
        NameBox.Text = "";
        ReleaseDatePicker.SelectedDate = null;
        ExecutableBox.Text = "";
        ArgumentsBox.Text = "";
        WorkingDirectoryBox.Text = "";
        DescriptionBox.Text = "";
        PublisherBox.Text = "";
        GenreBox.Text = "";
        DeveloperBox.Text = "";
        WorkingDirectoryBox.Text = "";
        UseShellExecuteBox.IsChecked = true;
        CoverImage.Source = null;
        ScreenshotsList.Items.Clear();
        _currentGame.PendingCoverBytes = null;
        _currentGame.PendingCoverBitmap = null;
        _currentGame.PendingCoverUrl = null;
    }
    private Window? GetOwnerWindow()
    {
        return TopLevel.GetTopLevel(this) as Window;
    }

    private async void SearchIgdbButton_Click(object? sender, RoutedEventArgs e)
    {
        IGDBLookupWindow lookup = new(NameBox.Text);

        Window? owner = TopLevel.GetTopLevel(this) as Window;

        if (owner == null)
            return;

        await lookup.ShowDialog(owner);
        Debug.WriteLine($"After Dialog: {lookup.SelectedGame?.Name ?? "NULL"}");
        if (lookup.SelectedGame == null)
            return;

        await ApplyIGDBGame(lookup.SelectedGame);
    }

    private async Task ApplyIGDBGame(IGDBGame selectedGame)
    {
        Debug.WriteLine($"ApplyIGDBGame: {_currentGame?.AppId}");
        Debug.WriteLine($"Selected: {selectedGame?.Name}");

        if (selectedGame == null || _currentGame == null)
            return;


        _currentGame.Helper ??= new MetaHelper
        {
            Type = "Steam"
        };
        // Save helper metadata
        _currentGame.Helper.IGDBId = selectedGame.Id;
        _currentGame.Helper.SteamGridDBId = null;

        // Update current game
        _currentGame.Meta.Name = selectedGame.Name;
        _currentGame.Meta.ShortDescription = selectedGame.Summary;

        if (selectedGame.FirstReleaseDate.HasValue)
        {
            DateTime date = DateTimeOffset
                .FromUnixTimeSeconds(selectedGame.FirstReleaseDate.Value)
                .DateTime;

            _currentGame.Meta.ReleaseDate = date.ToString("yyyy-MM-dd");

            ReleaseDatePicker.SelectedDate = date;
        }
        else
        {
            _currentGame.Meta.ReleaseDate = "";
            ReleaseDatePicker.SelectedDate = null;
        }

        _currentGame.Meta.Developers = selectedGame.InvolvedCompanies
            .Where(c => c.Developer && c.Company != null)
            .Select(c => c.Company!.Name)
            .ToList();

        _currentGame.Meta.Publishers = selectedGame.InvolvedCompanies
            .Where(c => c.Publisher && c.Company != null)
            .Select(c => c.Company!.Name)
            .ToList();

        _currentGame.Meta.Genres = selectedGame.Genres
            .Select(g => g.Name)
            .ToList();

        // Update UI
        NameBox.Text = _currentGame.Meta.Name;
        DescriptionBox.Text = _currentGame.Meta.ShortDescription;

        DeveloperBox.Text = string.Join(", ", _currentGame.Meta.Developers);
        PublisherBox.Text = string.Join(", ", _currentGame.Meta.Publishers);
        GenreBox.Text = string.Join(", ", _currentGame.Meta.Genres);

        await DownloadScreenshotsAsync(selectedGame);
    }
    private static readonly HttpClient _http = new();
    private async Task DownloadScreenshotsAsync(IGDBGame selectedGame)
    {
        if (_currentGame == null)
            return;

        _currentGame.PendingScreenshotBytes.Clear();
        _currentGame.PendingScreenshotBitmaps.Clear();
        _currentGame.Meta.ScreenshotUrls.Clear();

        ScreenshotsList.Items.Clear();
        foreach (IGDBImage screenshot in selectedGame.Screenshots.Take(3))
        {
            string url = screenshot.GetUrl("t_720p");

            if (string.IsNullOrWhiteSpace(url))
                continue;

            byte[] bytes = await _http.GetByteArrayAsync(url);

            using MemoryStream ms = new(bytes);
            Bitmap bitmap = new(ms);

            _currentGame.PendingScreenshotBytes.Add(bytes);
            _currentGame.PendingScreenshotBitmaps.Add(bitmap);
            _currentGame.Meta.ScreenshotUrls.Add(url);

            ScreenshotsList.Items.Add(new Image
            {
                Width = 240,
                Height = 135,
                Stretch = Avalonia.Media.Stretch.UniformToFill,
                Source = bitmap
            });
        }
    }

    private async void SteamGridDBButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentGame == null)
            return;

        SteamGridDBWindow window = new();

        Window? owner = GetOwnerWindow();

        if (owner == null)
            return;

        await window.LoadImagesAsync(_currentGame.Meta.Name);

        await window.ShowDialog(owner);

        if (window.SelectedImage == null)
            return;

        await ApplySteamGridCover(window.SelectedImage);
    }

    private async Task ApplySteamGridCover(SteamGridImage image)
    {
        if (_currentGame == null)
            return;

        using HttpClient client = new();

        byte[] bytes = await client.GetByteArrayAsync(image.Url);

        using MemoryStream ms = new(bytes);

        Bitmap bitmap = new(ms);

        // Store pending changes only
        _currentGame.PendingCoverBytes = bytes;
        _currentGame.PendingCoverBitmap = bitmap;
        _currentGame.PendingCoverUrl = image.Url;

        // Preview immediately
        CoverImage.Source = bitmap;
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentGame == null)
            return;

        // Update from UI
        _currentGame.Meta.Name = NameBox.Text?.Trim() ?? "";
        _currentGame.Meta.ShortDescription = DescriptionBox.Text?.Trim() ?? "";

        _currentGame.Meta.ReleaseDate =
            ReleaseDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

        _currentGame.Meta.Developers =
            DeveloperBox.Text?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new();

        _currentGame.Meta.Publishers =
            PublisherBox.Text?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new();

        _currentGame.Meta.Genres =
            GenreBox.Text?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList() ?? new();

        _currentGame.Launch ??= new LaunchInfo();

        _currentGame.Launch.Executable =
            ExecutableBox.Text?.Trim() ?? "";

        _currentGame.Launch.WorkingDirectory =
            WorkingDirectoryBox.Text?.Trim() ?? "";

        _currentGame.Launch.Arguments =
            ArgumentsBox.Text?.Trim() ?? "";

        _currentGame.Launch.UseShellExecute =
            UseShellExecuteBox.IsChecked ?? true;

        SaveButton.IsEnabled = false;

        try
        {
            CacheRespitory repo = new();

            await repo.SaveGameAsync(_currentGame);

            EditorStatus.Text = "Game saved successfully.";


            GameSaved?.Invoke(_currentGame);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void BrowseExeButton_Click(object? sender, RoutedEventArgs e)
    {
        Window? owner = GetOwnerWindow();

        if (owner == null)
            return;

        var files = await owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select Executable",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Executable")
                {
                    Patterns = OperatingSystem.IsWindows()
                        ? new[] { "*.exe" }
                        : new[] { "*" }
                },
                FilePickerFileTypes.All
                ]
            });

        string? path = files.FirstOrDefault()?.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            ExecutableBox.Text = path;
            WorkingDirectoryBox.Text = Path.GetDirectoryName(path) ?? "";
        }
    }

    private async void BrowseWorkingDirButton_Click(object? sender, RoutedEventArgs e)
    {
        Window? owner = GetOwnerWindow();

        if (owner == null)
            return;

        var folder = await owner.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Working Directory",
                AllowMultiple = false
            });

        string? path = folder.FirstOrDefault()?.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            WorkingDirectoryBox.Text = path;
        }
    }

    private async void BrowseCoverButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentGame == null)
            return;

        Window? owner = GetOwnerWindow();

        if (owner == null)
            return;

        var files = await owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Select Cover Image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                {
                    Patterns = new[]
                    {
                        "*.jpg",
                        "*.jpeg",
                        "*.png",
                        "*.webp"
                    }
                },
                FilePickerFileTypes.All
                ]
            });

        string? path = files.FirstOrDefault()?.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        _currentGame.PendingCoverBytes = await File.ReadAllBytesAsync(path);
        _currentGame.PendingCoverBitmap = new Bitmap(path);

        CoverImage.Source = _currentGame.PendingCoverBitmap;
    }

    private void BoxroomPlusLink_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/MidgetBrony/Boxroom-Plus",
            UseShellExecute = true
        });
    }
}