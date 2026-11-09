namespace MovieScrap2000;

public static class LibrarySources
{
    public static StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static List<string> Normalize(IEnumerable<string>? paths) => (paths ?? [])
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim())))
        .Distinct(PathComparer).ToList();

    public static IReadOnlyList<string> GetFolders(IEnumerable<string> roots, CancellationToken token, Action<string, Exception> reportError)
    {
        var folders = new HashSet<string>(PathComparer);
        foreach (var root in Normalize(roots))
        {
            token.ThrowIfCancellationRequested();
            try { foreach (var folder in Directory.GetDirectories(root)) folders.Add(folder); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { reportError(root, ex); }
        }
        return folders.ToArray();
    }

    public static bool ContainsFolder(IEnumerable<string> roots, string folder)
    {
        var fullPath = Path.GetFullPath(folder);
        return Normalize(roots).Any(root =>
        {
            var relative = Path.GetRelativePath(root, fullPath);
            return !Path.IsPathRooted(relative) && relative != ".."
                && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        });
    }
}
