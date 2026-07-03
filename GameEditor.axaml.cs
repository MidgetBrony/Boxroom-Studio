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

    /// <summary>
    /// Shows the game editor UI and hides the empty state message. This method is typically called when a game is selected for editing, allowing the user to view and modify the game's details.
    /// </summary>
    public void ShowEditor()
    {
        EmptyState.IsVisible = false;
        EditorRoot.IsVisible = true;
    }
    /// <summary>
    /// Shows the empty state message and hides the game editor UI. This method is typically called when no game is selected for editing, providing a visual indication that the user should select or create a game to edit.
    /// </summary>
    public void ShowEmpty()
    {
        EmptyState.IsVisible = true;
        EditorRoot.IsVisible = false;
    }
    /// <summary>
    /// Loads the provided CacheGame into the editor, populating the UI fields with the game's metadata, launch information, cover image, and screenshots. This method also resets any pending cover or screenshot changes to ensure that the editor reflects the current state of the game being edited.
    /// </summary>
    /// <param name="game"></param>
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

    /// <summary>
    /// Initializes a new game with default values and clears the UI fields. This method sets up a new CacheGame instance with a unique AppId, initializes its metadata, launch information, and helper data. It also resets the UI elements to their default state, allowing the user to input new game information.
    /// </summary>
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

    /// <summary>
    /// Opens the IGDB lookup window to search for game metadata based on the current game's name. If a game is selected, it applies the selected IGDB game metadata to the current game.
    /// </summary>
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

    /// <summary>
    /// Applies the selected IGDB game metadata to the current game. This method updates the current game's metadata fields, including name, description, release date, developers, publishers, and genres. It also downloads and prepares screenshots for immediate preview in the UI.
    /// </summary>
    /// <param name="selectedGame"></param>
    /// <returns></returns>
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
    /// <summary>
    /// A static HttpClient instance used for downloading screenshots. This instance is shared across all instances of the GameEditor class to improve performance and reduce resource usage.
    /// </summary>
    private static readonly HttpClient _http = new();

    /// <summary>
    /// Downloads screenshots for the selected IGDB game and updates the current game's pending screenshot properties. This method retrieves up to three screenshots, downloads them, and prepares them for immediate preview in the UI.
    /// </summary>
    /// <param name="selectedGame"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Handles the click event for the SteamGridDB button. This method opens a SteamGridDB window to search for cover images based on the current game's name. If an image is selected, it applies the selected cover image to the current game.
    /// </summary>
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

    /// <summary>
    /// Applies the selected SteamGridDB cover image to the current game. This method downloads the image from the provided URL and updates the current game's pending cover properties, allowing for immediate preview in the UI.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Handles the click event for the Save button. This method saves the current game's metadata and launch information.
    /// </summary>
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

    /// <summary>
    /// Handles the click event for the Browse Executable button. This method opens a file picker to select an executable file for the current game.
    /// </summary>
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

    /// <summary>
    /// Handles the click event for the Browse Working Directory button. This method opens a folder picker to select a working directory for the current game.
    /// </summary>
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

    /// <summary>
    /// Handles the click event for the Browse Cover button. This method opens a file picker to select a cover image for the current game.
    /// </summary>
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

    /// <summary>
    /// Handles the click event for the Boxroom-Plus link. This method opens the Boxroom-Plus GitHub page in the default web browser.
    /// </summary>
    private void BoxroomPlusLink_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/MidgetBrony/Boxroom-Plus",
            UseShellExecute = true
        });
    }
    /// <summary>
    /// Event that is triggered when a game is deleted. Subscribers to this event will receive the deleted CacheGame object as an argument.
    /// </summary>
    public static event Action<CacheGame>? GameDeleted;
    /// <summary>
    /// Handles the click event for the Delete button. This method is currently empty and should be implemented to handle the deletion of the current game.
    /// </summary>
    private async void DeleteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentGame == null)
            return;

        if (string.IsNullOrWhiteSpace(_currentGame.Folder))
            return;

        Window? owner = GetOwnerWindow();

        // confirmation...

        DeleteButton.IsEnabled = false;

        try
        {
            CacheRespitory repo = new();

            await repo.DeleteGameAsync(_currentGame);

            GameDeleted?.Invoke(_currentGame);

            ShowEmpty();
        }
        finally
        {
            DeleteButton.IsEnabled = true;
        }
    }
}