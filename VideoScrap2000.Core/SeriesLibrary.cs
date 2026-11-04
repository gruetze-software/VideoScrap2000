using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MovieScrap2000;

public sealed record SeriesEpisode(string VideoPath, string NfoPath, XElement Metadata, bool HasNfo);
public sealed record SeriesShow(string FolderPath, string NfoPath, XElement Metadata, IReadOnlyList<SeriesEpisode> Episodes, bool HasNfo);

public static partial class SeriesLibrary
{
    [GeneratedRegex(@"(?<!\d)(?:S(?<season>\d{1,3})[ ._-]*E|(?<season>\d{1,3})x)(?<episode>\d{1,4})(?!\d)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EpisodeNumber();
    [GeneratedRegex(@"^(?:season|staffel)[ ._-]*(\d{1,3})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SeasonFolder();
    [GeneratedRegex(@"^(?:E)?(\d{1,3})(?:[ ._-]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LeadingEpisodeNumber();

    public static IReadOnlyList<SeriesShow> Scan(string root, CancellationToken token)
    {
        var results = new List<SeriesShow>();
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            token.ThrowIfCancellationRequested();
            var nfo = Path.Combine(folder, "tvshow.nfo");
            var hasNfo = File.Exists(nfo);
            var episodes = new List<SeriesEpisode>();
            foreach (var file in Directory.EnumerateFiles(folder, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            }))
            {
                token.ThrowIfCancellationRequested();
                if (MovieManager.VideoExtensions.Contains(Path.GetExtension(file).ToLowerInvariant())) episodes.Add(ReadEpisode(file));
            }
            if (episodes.Count == 0 && !hasNfo) continue;
            var metadata = ReadMetadata(nfo, "tvshow");
            metadata.SetElementValue("title", (string?)metadata.Element("title") ?? Path.GetFileName(folder));
            results.Add(new(folder, nfo, metadata, episodes, hasNfo));
        }
        return results.OrderBy(show => (string?)show.Metadata.Element("title"), StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public static SeriesEpisode ReadEpisode(string video)
    {
        var nfo = Path.ChangeExtension(video, ".nfo");
        var metadata = ReadMetadata(nfo, "episodedetails");
        var name = Path.GetFileNameWithoutExtension(video);
        var match = EpisodeNumber().Match(name);
        metadata.SetElementValue("title", (string?)metadata.Element("title") ?? name);
        if (match.Success)
        {
            SetInferredNumber(metadata, "season", match.Groups["season"].Value);
            SetInferredNumber(metadata, "episode", match.Groups["episode"].Value);
        }
        else
        {
            var parent = Path.GetFileName(Path.GetDirectoryName(video)) ?? "";
            var season = SeasonFolder().Match(parent);
            var episode = LeadingEpisodeNumber().Match(name);
            if (season.Success && episode.Success)
            {
                SetInferredNumber(metadata, "season", season.Groups[1].Value);
                SetInferredNumber(metadata, "episode", episode.Groups[1].Value);
            }
        }
        return new(video, nfo, metadata, File.Exists(nfo));
    }

    private static void SetInferredNumber(XElement metadata, string field, string number)
    {
        if (metadata.Element(field) is null)
            metadata.SetElementValue(field, int.Parse(number, CultureInfo.InvariantCulture));
    }

    private static XElement ReadMetadata(string path, string kind)
    {
        if (File.Exists(path))
        {
            try
            {
                var root = MovieManager.LoadExistingNfoDocument(path).Root;
                if (root?.Name == kind) return new(root);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException or DecoderFallbackException)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
        }
        return new(kind);
    }

    public static async Task SaveAsync(string path, XElement changes, IReadOnlyCollection<string> fields, CancellationToken token)
    {
        var existed = File.Exists(path);
        var creationTime = existed && OperatingSystem.IsWindows() ? File.GetCreationTimeUtc(path) : (DateTime?)null;
        var document = existed ? MovieManager.LoadExistingNfoDocument(path) : new XDocument(new XElement(changes.Name));
        var root = document.Root;
        if (root is null || root.Name != changes.Name) throw new InvalidDataException("Unexpected series NFO document type.");
        foreach (var field in fields)
        {
            if (field == "uniqueid")
            {
                foreach (var id in changes.Elements(field))
                {
                    var type = (string?)id.Attribute("type");
                    var existing = root.Elements(field).FirstOrDefault(element => string.Equals((string?)element.Attribute("type"), type, StringComparison.OrdinalIgnoreCase));
                    if (id.Value.Length == 0) existing?.Remove();
                    else if (existing is null) root.Add(new XElement(id));
                    else existing.Value = id.Value;
                }
                continue;
            }
            root.Elements(field).Remove();
            root.Add(changes.Elements(field).Select(element => new XElement(element)));
        }
        var temporary = path + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, document.ToString(), new UTF8Encoding(false), token);
            if (creationTime is { } created) File.SetCreationTimeUtc(temporary, created);
            token.ThrowIfCancellationRequested();
            if (existed) File.Copy(path, path + ".bak", overwrite: true);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
