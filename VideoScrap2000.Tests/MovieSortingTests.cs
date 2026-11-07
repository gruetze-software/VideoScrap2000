using System.Reflection;
using Avalonia.Headless.XUnit;
using MovieScrap2000;
using VideoScrap2000.ViewModels;
using Xunit;

namespace VideoScrap2000.Tests;
public class MovieSortingTests
{
    [AvaloniaFact]
    public void SortChangesKeepNewMoviesFirstAndPreserveSelection()
    {
        using var model = new MainViewModel(settings: new AppSettings());
        MovieViewModel Movie(string title, string year, int day, bool isNew = false)
        {
            var movie = new MovieViewModel { Title = title, Year = year };
            movie.UpdateFileFacts(new MovieFileFacts(new DateTime(2020, 1, 5 - day), !isNew, true)
            {
                NfoCreatedAtUtc = isNew ? null : new DateTime(2024, 1, day, 0, 0, 0, DateTimeKind.Utc)
            });
            return movie;
        }
        var alpha = Movie("Alpha", "2022", 1);
        var beta = Movie("Beta", "2000", 3);
        var unknown = Movie("Unknown", "", 2);
        var fresh = Movie("Zulu new", "1990", 1, true);
        var movies = new List<MovieViewModel> { alpha, beta, unknown, fresh };
        typeof(MainViewModel).GetField("allMovies", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(model, movies);
        typeof(MainViewModel).GetField("selectedMovie", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(model, beta);
        model.Query = " ";
        Assert.Equal(new[] { fresh, beta, unknown, alpha }, model.Movies);
        model.MovieSortIndex = 1;
        Assert.Equal(new[] { fresh, alpha, beta, unknown }, model.Movies);
        model.MovieSortIndex = 2;
        Assert.Equal(new[] { fresh, alpha, beta, unknown }, model.Movies);
        Assert.Same(beta, model.SelectedMovie);
        Assert.True(fresh.MissingMetadata);
        model.Query = "Beta";
        Assert.Equal(new[] { beta }, model.Movies);
    }
    [AvaloniaFact]
    public async Task ReloadPublishesOnlyDatedMoviesInTheChosenOrder()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = new DatabaseManager(Path.Combine(root, "cache.sqlite"));
        try
        {
            await database.InitializeDatabaseAsync();
            using var service = new MetadataService(database);
            foreach (var (name, year, day) in new[] { ("Alpha", "2024", 1), ("Zulu", "2001", 3), ("Middle", "2010", 2) })
            {
                var folder = Path.Combine(root, name); Directory.CreateDirectory(folder);
                var video = Path.Combine(folder, name + ".mkv"); File.WriteAllBytes(video, [0]);
                File.SetLastWriteTime(video, new DateTime(2020, 1, day));
                await service.SaveMovieAsync(new Movie { Title = name, Year = year }, folder, video, false, TestContext.Current.CancellationToken);
                if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) File.SetCreationTimeUtc(Path.ChangeExtension(video, ".nfo"), new DateTime(2024, 1, day, 0, 0, 0, DateTimeKind.Utc));
            }
            DateTime NfoCreation(string name) => File.GetCreationTimeUtc(Path.Combine(root, name, name + ".nfo"));
            DateTime? Captured(string name) => MovieFileFacts.Read(Path.Combine(root, name), Path.Combine(root, name, name + ".mkv")).NfoCreatedAtUtc;
            string[] ExpectedOrder() => new[] { "Alpha", "Middle", "Zulu" }.OrderByDescending(Captured).ThenBy(name => name).ToArray();
            using var model = new MainViewModel(database, new AppSettings());
            await model.InitializeAsync();
            Assert.Equal(ExpectedOrder(), model.Movies.Select(m => m.Title));
            var selected = model.SelectedMovie!.Id;
            var publishedDates = new List<bool>();
            model.Movies.CollectionChanged += (_, e) =>
            {
                if (e.NewItems is not null)
                    foreach (MovieViewModel movie in e.NewItems) publishedDates.Add(movie.MetadataCapturedAtUtc.HasValue);
            };
            var alphaFolder = Path.Combine(root, "Alpha");
            var alphaCreation = NfoCreation("Alpha");
            await service.SaveMovieAsync(new Movie { Title = "Alpha", Year = "2024" }, alphaFolder, Path.Combine(alphaFolder, "Alpha.mkv"), false, TestContext.Current.CancellationToken);
            await model.ReloadAsync();
            Assert.NotEmpty(publishedDates);
            Assert.All(publishedDates, Assert.True);
            Assert.Equal(ExpectedOrder(), model.Movies.Select(m => m.Title));
            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()) Assert.Equal(alphaCreation, NfoCreation("Alpha"));
            Assert.Equal(alphaCreation, model.Movies.Single(movie => movie.Title == "Alpha").MetadataCapturedAtUtc);
            Assert.Equal(selected, model.SelectedMovie!.Id);
            model.MovieSortIndex = 1;
            await model.ReloadAsync();
            Assert.Equal(new[] { "Alpha", "Middle", "Zulu" }, model.Movies.Select(m => m.Title));
            model.MovieSortIndex = 2;
            await model.ReloadAsync();
            Assert.Equal(new[] { "Alpha", "Middle", "Zulu" }, model.Movies.Select(m => m.Title));
            for (var i = 0; !model.IsDetailReady && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);

            var untouched = model.Movies.Where(movie => movie.Id != selected).ToArray();
            model.Title = "Zulu updated";
            await model.SaveCommand.ExecuteAsync(null);
            Assert.Equal(selected, model.SelectedMovie!.Id);
            Assert.Equal("Zulu updated", model.SelectedMovie.Title);
            Assert.Equal(untouched, model.Movies.Where(movie => movie.Id != selected));
            for (var i = 0; !model.IsDetailReady && i < 200; i++) await Task.Delay(10, TestContext.Current.CancellationToken);
            Assert.True(model.IsDetailReady);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root, true); }
    }
}
