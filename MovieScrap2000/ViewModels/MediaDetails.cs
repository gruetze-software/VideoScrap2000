using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieScrap2000;

namespace VideoScrap2000.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private MovieFileInfo? mediaInfo;
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task RefreshMediaInfoAsync()
    {
        if (IsDemo) { Status = "Die Demo verwendet ausschließlich mitgelieferte Beispieldaten."; return; }
        if (SelectedMovie is not { } movie) return;
        await RunAsync(async token =>
        {
            Status = "Technische Mediendaten werden gelesen …";
            var info = await TechnicalMediaReader.ReadAsync(movie.VideoFilePath, token);
            if (info is null) { Status = "Technische Mediendaten konnten nicht gelesen werden."; return; }
            if (!ReferenceEquals(movie, SelectedMovie)) return;
            if (movie.FullMovieNFO is { } nfo) nfo.FileInfo = info;
            MediaInfo = info;
            TechnicalDetails = Localization.Locale.T(DescribeStreams(info));
            Status = "Mediendetails neu eingelesen. Mit Änderungen speichern übernehmen.";
        }, reloadAfterCancellation: false);
    }
}

public partial class SeriesViewModel
{
    [RelayCommand]
    private async Task RefreshEpisodeMediaAsync()
    {
        if (UseDemoData() || SelectedEpisode is not { } episode) return;
        await main.RunAsync(async token =>
        {
            main.Status = "Technische Mediendaten werden gelesen …";
            var info = await TechnicalMediaReader.ReadAsync(episode.Episode.VideoPath, token);
            if (info is null) { main.Status = "Technische Mediendaten konnten nicht gelesen werden."; return; }
            episode.Editor.MediaInfo = info;
            main.Status = "Mediendetails neu eingelesen. Mit Änderungen speichern übernehmen.";
        }, reloadAfterCancellation: false);
    }
}
