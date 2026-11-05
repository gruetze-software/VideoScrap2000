using System.Globalization;
using System.Xml.Linq;
using Avalonia.Platform;
using MovieScrap2000;
using SkiaSharp;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000;

/// <summary>A fully local, fictional collection, separate from the user's cache and settings.</summary>
internal static class DemoLibrary
{
    internal const string SignalPlot = "When a long-silent radio observatory receives a signal repeating every seventeen minutes, " +
        "astronomer Mara Vale returns to the mountain station she left years earlier. The transmission contains fragments of " +
        "a conversation that has not happened yet. With a winter storm cutting off the access road, Mara and a small maintenance " +
        "crew must decide whether the message is a warning, an experiment, or an elaborate deception. As the next transmission " +
        "approaches, an unexpected visitor challenges everything they believe about the source of the signal.";

    public static MainViewModel CreateModel(string? root = null)
    {
        root ??= Path.Combine(Utils.GetOptionsPath(), "Demo", "v1");
        Seed(root);
        return new(new DatabaseManager(Path.Combine(root, "demo.sqlite")), new AppSettings
        {
            LibraryPath = Path.Combine(root, "Movies"), SeriesPath = Path.Combine(root, "TV Shows"),
            Language = "en", MetadataLanguage = "en", ArtworkLanguage = "en",
            Theme = "Dark", UseFanartBackground = true
        }, isDemo: true);
    }

    internal static void Seed(string root)
    {
        Directory.CreateDirectory(root);
        var portraits = Path.Combine(root, "Cast");
        Directory.CreateDirectory(portraits);
        var portrait = Path.Combine(portraits, "Elena Ward.jpg");
        CopyAsset("actor", portrait);
        var movies = new[]
        {
            new DemoMovie("The Last Signal", "signal", 2025, 8.4f, "Some messages arrive before their time.", "Science Fiction", "Mystery", SignalPlot),
            new DemoMovie("North of Tomorrow", "tomorrow", 2024, 7.6f, "Every journey begins with a return.", "Drama", "Adventure",
                "A coastal cartographer returns home to restore her father's unfinished map. Following the places he marked, she discovers the stories of a changing community and a chance to rebuild a friendship."),
            new DemoMovie("Beyond the Quiet Sky", "quiet-sky", 2023, 8.1f, "Listen to what remains.", "Drama", "Science Fiction",
                "Two engineers at a remote observatory prepare to close the station for good. An unexplained recording brings an old disagreement back into focus during their final night on the mountain."),
            new DemoMovie("The Copper Tide", "copper-tide", 2025, 7.2f, "The harbor keeps its promises.", "Thriller", "Mystery",
                "A marine surveyor finds an abandoned research buoy drifting into a quiet harbor. Its recordings lead her through a web of conflicting accounts about a ship that never arrived."),
            new DemoMovie("Seventeen Minutes", "seventeen", 2022, 6.8f, "A small window. A different future.", "Thriller", "Science Fiction",
                "An emergency technician receives a call from a station listed as empty. She has seventeen minutes to reach it before an approaching storm interrupts every connection."),
            new DemoMovie("Letters from the Fjord", "letters", 2021, 8.7f, "Some stories find their way home.", "Drama", "Romance",
                "A bookbinder discovers a box of undelivered letters during the renovation of a harbor post office. Returning them brings together neighbors whose lives have quietly crossed for decades.")
        };
        for (var index = 0; index < movies.Length; index++)
        {
            var item = movies[index];
            var folder = Path.Combine(root, "Movies", $"{item.Title} ({item.Year})");
            Directory.CreateDirectory(folder);
            var basename = Path.Combine(folder, item.Title);
            File.WriteAllBytes(basename + ".mkv", []);
            var nfo = Metadata("movie", item.Title, item.Plot, portrait);
            nfo.Add(new XElement("year", item.Year), new XElement("premiered", $"{item.Year}-09-12"),
                new XElement("id", $"tt{9900001 + index}"), new XElement("tmdb", 9900001 + index),
                new XElement("rating", item.Rating.ToString(CultureInfo.InvariantCulture)), new XElement("runtime", 108 + index * 4),
                new XElement("tagline", item.Tagline), new XElement("genre", item.Genre), new XElement("genre", item.OtherGenre),
                new XElement("mpaa", "PG-13"), new XElement("country", "United Kingdom"),
                new XElement("director", "Rowan Ellis"), new XElement("credits", "Avery Quinn"),
                new XElement("dateadded", new DateTime(2026, 10, 28).AddDays(-index).ToString("yyyy-MM-dd HH:mm:ss")),
                new XElement("fileinfo", new XElement("streamdetails",
                    new XElement("video", new XElement("codec", "hevc"), new XElement("width", 3840), new XElement("height", 2160),
                        new XElement("aspect", "1.7778"), new XElement("durationinseconds", (108 + index * 4) * 60)),
                    new XElement("audio", new XElement("codec", "eac3"), new XElement("language", "eng"), new XElement("channels", 6)),
                    new XElement("subtitle", new XElement("language", "eng")))));
            if (index != 0) nfo.Add(new XElement("outline", "An unexpected discovery brings a new perspective to a familiar place."));
            Save(nfo, basename + ".nfo");
            if (OperatingSystem.IsWindows()) File.SetCreationTime(basename + ".nfo", new DateTime(2026, 10, 28).AddDays(-index));
            CreateArtwork(item.Scene, item.Title, item.Tagline, basename + "-poster.jpg", 600, 900, poster: true);
            CopyAsset(item.Scene, basename + "-fanart.jpg");
            CreateArtwork(item.Scene, item.Title, "", basename + "-landscape.jpg", 960, 540);
            CreateArtwork(item.Scene, item.Title, "", basename + "-banner.jpg", 1200, 222);
        }
        CreateShow(root, "North Harbor", "harbor", portrait,
            "When a research vessel returns without its captain, harbor detective Iris Holt follows a trail of interrupted messages through a close-knit coastal town. Each tide reveals another piece of a story the community has kept to itself.",
            ["The Returning Vessel", "Low Water", "A Light Across the Bay", "The Missing Log", "Before the Storm", "The Northern Route"]);
        CreateShow(root, "Station Meridian", "signal", portrait,
            "A small team runs a mountain observatory scheduled for closure. Between routine repairs and late-night observations, an unusual pattern draws them into a mystery that reaches far beyond the station.",
            ["First Contact", "The Night Shift", "Seventeen Minutes", "Whiteout", "Echoes", "The Open Channel"]);
        File.WriteAllText(Path.Combine(root, "ABOUT-DEMO.txt"),
            "VideoScrap 2000 fictional demonstration collection. Titles, people, studios, plots and technical details are invented. " +
            "Artwork was generated specifically for this demo. The .mkv files are empty placeholders, not playable videos. " +
            "Reopening demo mode restores the example metadata. No external metadata services are used.");
    }

    private static XElement Metadata(string kind, string title, string plot, string portrait) => new(kind,
        new XElement("title", title), new XElement("originaltitle", title), new XElement("plot", plot),
        new XElement("studio", "Meridian Lantern Pictures"),
        new XElement("actor", new XElement("name", "Elena Ward"), new XElement("role", "Mara Vale"), new XElement("thumb", portrait), new XElement("order", 0)),
        new XElement("actor", new XElement("name", "Jonah Reed"), new XElement("role", "Elias Stone"), new XElement("order", 1)),
        new XElement("actor", new XElement("name", "Mira Solberg"), new XElement("role", "Dr. Iris Holt"), new XElement("order", 2)));

    private static void CreateShow(string root, string title, string scene, string portrait, string plot, string[] titles)
    {
        var folder = Path.Combine(root, "TV Shows", title);
        Directory.CreateDirectory(folder);
        var nfo = Metadata("tvshow", title, plot, portrait);
        nfo.Add(new XElement("premiered", "2024-11-08"), new XElement("status", "Continuing"),
            new XElement("uniqueid", new XAttribute("type", "tmdb"), scene == "harbor" ? 9900101 : 9900102),
            new XElement("mpaa", "TV-14"), new XElement("genre", "Mystery"), new XElement("genre", "Drama"), new XElement("country", "Norway"));
        Save(nfo, Path.Combine(folder, "tvshow.nfo"));
        CopyAsset(scene, Path.Combine(folder, "fanart.jpg"));
        CreateArtwork(scene, title, "AN ORIGINAL SERIES", Path.Combine(folder, "poster.jpg"), 600, 900, poster: true);
        CreateArtwork(scene, title, "", Path.Combine(folder, "banner.jpg"), 1200, 222);
        CreateArtwork(scene, title, "", Path.Combine(folder, "landscape.jpg"), 960, 540);
        for (var season = 1; season <= 2; season++)
        {
            CreateArtwork(scene, title, $"SEASON {season}", Path.Combine(folder, $"season{season:00}-poster.jpg"), 600, 900, poster: true);
            CreateArtwork(scene, title, $"SEASON {season}", Path.Combine(folder, $"season{season:00}-banner.jpg"), 1200, 222);
            var seasonFolder = Path.Combine(folder, $"Season {season:00}");
            Directory.CreateDirectory(seasonFolder);
            for (var episode = 1; episode <= titles.Length; episode++)
            {
                var basename = Path.Combine(seasonFolder, $"{title} S{season:00}E{episode:00}");
                File.WriteAllBytes(basename + ".mkv", []);
                var details = Metadata("episodedetails", titles[episode - 1],
                    $"{(scene == "harbor" ? "Iris" : "Mara")} follows a new lead while the team compares two conflicting accounts. " +
                    "An unexpected arrival changes the course of the investigation, and a familiar location takes on a different meaning.", portrait);
                details.Add(new XElement("season", season), new XElement("episode", episode),
                    new XElement("uniqueid", new XAttribute("type", "tmdb"), 9901000 + season * 100 + episode),
                    new XElement("aired", new DateTime(2024 + season - 1, 11, 8).AddDays((episode - 1) * 7).ToString("yyyy-MM-dd")),
                    new XElement("runtime", 48 + episode), new XElement("director", "Rowan Ellis"), new XElement("credits", "Avery Quinn"),
                    new XElement("rating", "8.2"));
                Save(details, basename + ".nfo");
                CopyAsset(scene, basename + "-thumb.jpg");
            }
        }
    }

    private static void Save(XElement metadata, string path) => new XDocument(metadata).Save(path);
    private static void CopyAsset(string scene, string path)
    {
        using var source = AssetLoader.Open(new Uri($"avares://VideoScrap2000/Assets/Demo/{scene}.jpg"));
        using var destination = File.Create(path);
        source.CopyTo(destination);
    }

    private static void CreateArtwork(string scene, string title, string subtitle, string path, int width, int height, bool poster = false)
    {
        using var source = AssetLoader.Open(new Uri($"avares://VideoScrap2000/Assets/Demo/{scene}.jpg"));
        using var photo = SKBitmap.Decode(source);
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        var scale = Math.Max((float)width / photo.Width, (float)height / photo.Height);
        var drawnWidth = photo.Width * scale;
        var drawnHeight = photo.Height * scale;
        canvas.DrawBitmap(photo, new SKRect((width - drawnWidth) / 2, (height - drawnHeight) / 2,
            (width + drawnWidth) / 2, (height + drawnHeight) / 2));
        using var shade = new SKPaint { Shader = SKShader.CreateLinearGradient(new(0, 0), new(0, height),
            [new SKColor(8, 18, 30, 12), new SKColor(8, 18, 30, 235)], [0f, 1f], SKShaderTileMode.Clamp) };
        canvas.DrawRect(0, 0, width, height, shade);
        using var face = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        using var font = new SKFont(face, poster ? 57 : Math.Min(65, height * .28f));
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var lines = poster ? title.ToUpperInvariant().Split(' ').Chunk(2).Select(words => string.Join(' ', words)).ToArray() : [title.ToUpperInvariant()];
        foreach (var line in lines)
            while (font.MeasureText(line) > width - 80) font.Size -= 1;
        var y = poster ? height - 90 - (lines.Length - 1) * (font.Size + 10) : height * .55f;
        foreach (var line in lines) { canvas.DrawText(line, 40, y, font, paint); y += font.Size + 10; }
        if (subtitle.Length > 0)
        {
            font.Size = poster ? 19 : 22;
            while (font.MeasureText(subtitle) > width - 80) font.Size -= 1;
            paint.Color = new SKColor(150, 220, 214);
            canvas.DrawText(subtitle, 40, poster ? height - 36 : height * .8f, font, paint);
        }
        using var image = surface.Snapshot();
        using var bytes = image.Encode(SKEncodedImageFormat.Jpeg, 84);
        using var output = File.Create(path);
        bytes.SaveTo(output);
    }

    private sealed record DemoMovie(string Title, string Scene, int Year, float Rating, string Tagline, string Genre, string OtherGenre, string Plot);
}
