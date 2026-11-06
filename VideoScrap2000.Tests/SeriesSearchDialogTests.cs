using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MovieScrap2000.TMDbScraper;
using VideoScrap2000.Views;
using Xunit;

namespace VideoScrap2000.Tests;

public sealed class SeriesSearchDialogTests
{
    [AvaloniaFact]
    public void EmptyInitialSearchStaysOpenAndAllowsACorrectedTitle()
    {
        var queries = new List<string>();
        var match = new SearchTv { Id = 1, Name = "Scooby Doo", FirstAirDate = new DateTime(1969, 9, 13) };
        var dialog = new ScrapeWindow("Scoopy Doo", (query, _) =>
        {
            queries.Add(query);
            return Task.FromResult<IReadOnlyList<SearchTv>>(query == "Scooby Doo" ? [match] : []);
        });
        dialog.Show();
        try
        {
            Assert.Equal(["Scoopy Doo"], queries);
            Assert.True(dialog.IsVisible);
            Assert.False(dialog.FindControl<TextBox>("YearText")!.IsVisible);
            Assert.False(dialog.FindControl<Button>("AcceptButton")!.IsEnabled);
            dialog.FindControl<TextBox>("SearchText")!.Text = "Scooby Doo";
            dialog.FindControl<Button>("SearchButton")!.RaiseEvent(new(Button.ClickEvent));
            var row = Assert.IsType<MovieSearchResult>(dialog.FindControl<ListBox>("Results")!.SelectedItem);
            Assert.Same(match, row.Movie);
            Assert.Equal("Scooby Doo", row.Title);
            Assert.Equal("1969", row.Year);
            Assert.Equal(["Scoopy Doo", "Scooby Doo"], queries);
            Assert.Null(dialog.SeriesResult);
            dialog.FindControl<Button>("AcceptButton")!.RaiseEvent(new(Button.ClickEvent));
            Assert.Same(match, dialog.SeriesResult);
            Assert.False(dialog.IsVisible);
        }
        finally { dialog.Close(); }
    }

    [AvaloniaFact]
    public void ClosingCancelsAnInProgressSearch()
    {
        CancellationToken searchToken = default;
        var dialog = new ScrapeWindow("Example", async (_, token) =>
        {
            searchToken = token;
            await Task.Delay(Timeout.Infinite, token);
            return Array.Empty<SearchTv>();
        });
        dialog.Show();
        Assert.True(searchToken.CanBeCanceled);
        dialog.Close();
        Assert.True(searchToken.IsCancellationRequested);
        Assert.Null(dialog.SeriesResult);
    }
}
