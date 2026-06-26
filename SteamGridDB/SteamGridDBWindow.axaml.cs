using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Boxroom_Studio;

public partial class SteamGridDBWindow : Window
{
    private readonly SteamGridDBService _service = new();

    private static readonly HttpClient _client = new();

    private Border? _selectedBorder;

    public SteamGridImage? SelectedImage { get; private set; }

    public SteamGridDBWindow()
    {
        InitializeComponent();

        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Boxroom Studio/1.0");
    }

    public async Task LoadImagesAsync(string gameName)
    {
        CoverPanel.Children.Clear();

        List<SteamGridGame> games =
            await _service.SearchGamesAsync(gameName);

        if (games.Count == 0)
            return;

        // Later we'll let the user choose.
        SteamGridGame game = games[0];

        List<SteamGridImage> covers =
            await _service.GetGridsAsync(
                game.Id,
                dimensions: "600x900",
                limit: 20);

        await Task.WhenAll(
            covers.Select(AddCoverAsync));
    }

    private async Task AddCoverAsync(SteamGridImage cover)
    {
        Image image = new()
        {
            Width = 150,
            Height = 225,
            Stretch = Stretch.UniformToFill,
            Source = await DownloadBitmapAsync(cover.Thumb)
        };

        Border border = new()
        {
            Margin = new Thickness(6),
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.Transparent,
            Child = image,
            Tag = cover
        };

        border.PointerPressed += CoverClicked;

        // Optional: double-click = Use Selected
        border.DoubleTapped += (_, _) =>
        {
            SelectedImage = cover;
            Close();
        };

        CoverPanel.Children.Add(border);
    }

    private void CoverClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (_selectedBorder != null)
        {
            _selectedBorder.BorderBrush = Brushes.Transparent;
            _selectedBorder.BorderThickness = new Thickness(2);
        }

        _selectedBorder = border;

        border.BorderBrush = Brushes.DeepSkyBlue;
        border.BorderThickness = new Thickness(3);

        SelectedImage = (SteamGridImage)border.Tag!;
    }

    private static async Task<Bitmap> DownloadBitmapAsync(string url)
    {
        byte[] bytes = await _client.GetByteArrayAsync(url);

        using MemoryStream ms = new(bytes);

        return new Bitmap(ms);
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void UseSelectedButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedImage == null)
            return;

        Close();
    }
}