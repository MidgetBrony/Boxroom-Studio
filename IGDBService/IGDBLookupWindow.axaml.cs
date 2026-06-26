using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Boxroom_Studio;

public partial class IGDBLookupWindow : Window
{
    public IGDBGame? SelectedGame { get; private set; }
    private readonly IGDBService _service = new();
    private IGDBGame? _currentGame;
    public IGDBLookupWindow(string initialSearch = "")
    {
        InitializeComponent();
        SearchBox.Text = initialSearch ?? "";
    }
    public async Task SearchAsync()
    {
        string text = SearchBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
            return;

        SearchButton.IsEnabled = false;
        ResultsList.ItemsSource = null;

        try
        {
            var results = await _service.SearchGamesAsync(text);

            ResultsList.ItemsSource = results;
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private async void SearchButton_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            await SearchAsync();
        }
    }

    private async void SearchIgdbButton_Click(object? sender, RoutedEventArgs e)
    {
        await SearchAsync();
    }

    private async void ResultsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ResultsList.SelectedItem is not IGDBSearchResult result)
            return;

        _currentGame = await _service.LoadGameDetailsAsync(result.Id);

        if (_currentGame == null)
            return;

        PopulatePreview(_currentGame);
    }

    private void PopulatePreview(IGDBGame game)
    {
        GameName.Text = game.Name;

        SummaryBox.Text = game.Summary;

        Developer.Text = string.Join(", ",
            game.InvolvedCompanies
                .Where(c => c.Developer)
                .Select(c => c.Company?.Name));

        Publisher.Text = string.Join(", ",
            game.InvolvedCompanies
                .Where(c => c.Publisher)
                .Select(c => c.Company?.Name));

        Genres.Text = string.Join(", ",
            game.Genres.Select(g => g.Name));
    }

    private void UseSelectedButton_Click(object? sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"Before Close: {_currentGame?.Name}");
        if (_currentGame == null)
            return;

        SelectedGame = _currentGame;

        Close();
    }
}