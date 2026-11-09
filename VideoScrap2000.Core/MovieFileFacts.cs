namespace MovieScrap2000;

public sealed record MovieFileFacts(DateTime? Modified, bool HasMetadata, bool IsAvailable)
{
    // Linux cannot generally restore a file's birth time after an atomic rename.
    // Keep the original NFO creation time portable without changing Kodi dateadded.
    internal const string CreationAttribute = "videoscrap2000-created-utc";
    internal static DateTime? StoredCreationTime(string? value) => DateTime.TryParseExact(value, "O",
        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
        && date.Kind == DateTimeKind.Utc && date.Year >= 1900 ? date : null;

    private static DateTime? ReadCreationTime(FileInfo? nfo)
    {
        if (nfo is null) return null;
        try
        {
            using var reader = NfoXmlReader.Open(nfo.FullName);
            reader.MoveToContent();
            if (StoredCreationTime(reader.GetAttribute(CreationAttribute)) is { } stored) return stored;
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or IOException or UnauthorizedAccessException or System.Text.DecoderFallbackException) { }
        return nfo.CreationTimeUtc;
    }
    public DateTime? NfoCreatedAtUtc { get; init; }

    public static MovieFileFacts Read(string folder, string video)
    {
        if (!Directory.Exists(folder)) return new(null, false, false);
        try
        {
            var files = new DirectoryInfo(folder).EnumerateFiles().ToArray();
            var media = files.FirstOrDefault(f => string.Equals(f.FullName, video,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            return new(media?.LastWriteTime, files.Any(f => f.Extension.Equals(".nfo", StringComparison.OrdinalIgnoreCase)
                || f.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)), true)
            {
                NfoCreatedAtUtc = ReadCreationTime(MovieManager.FindNfoFile(files, video))
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(null, false, false); }
    }
}
