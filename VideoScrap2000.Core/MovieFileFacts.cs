namespace MovieScrap2000;

public sealed record MovieFileFacts(DateTime? Modified, bool HasMetadata, bool IsAvailable)
{
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
                NfoCreatedAtUtc = MovieManager.FindNfoFile(files, video)?.CreationTimeUtc
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(null, false, false); }
    }
}
