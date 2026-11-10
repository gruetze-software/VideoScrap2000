namespace MovieScrap2000;

public static class ArtworkFiles
{
    public static void RemoveNamed(string folder, IEnumerable<string> names, CancellationToken token)
    {
        var stems = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (stems.Any(name => string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name || name is "." or ".."))
            throw new ArgumentException("Ungültiger Artwork-Dateiname.");
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException(folder);
        foreach (var path in Directory.EnumerateFiles(folder))
        {
            token.ThrowIfCancellationRequested();
            if (stems.Contains(Path.GetFileNameWithoutExtension(path)) && Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp")
                File.Move(path, path + ".bak", true);
        }
    }

    public static IEnumerable<string> MovieStems(string video, string type)
    {
        if (!ImageTypes.AllTypes.Contains(type)) throw new ArgumentException("Unbekannter Bildtyp.");
        var suffix = type switch { ImageTypes.ClearLogo => "logo", ImageTypes.DiskArt => "discart", _ => type.ToLowerInvariant() };
        return new[] { Path.GetFileNameWithoutExtension(video) + "-" + suffix, suffix, type.ToLowerInvariant() }.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static IEnumerable<string> SeriesStems(string key)
    {
        yield return key;
        if (key is "poster" or "fanart") yield return "tvshow-" + key;
        if (key.StartsWith("season-specials-", StringComparison.OrdinalIgnoreCase)) yield return "season00-" + key[16..];
    }
    public static Task SaveAsync(string folder, string videoName, ImageCacheItem image, CancellationToken token)
    {
        var suffix = image.Type switch
        {
            ImageTypes.Poster => "poster", ImageTypes.Fanart => "fanart", ImageTypes.ClearLogo => "logo",
            ImageTypes.ClearArt => "clearart", ImageTypes.Banner => "banner", ImageTypes.Landscape => "landscape",
            ImageTypes.KeyArt => "keyart", ImageTypes.DiskArt => "discart", _ => throw new ArgumentException("Unbekannter Bildtyp.")
        };
        return SaveNamedAsync(folder, videoName + "-" + suffix, image.Data, token);
    }

    public static async Task<string> SaveNamedAsync(string folder, string name, byte[] bytes, CancellationToken token)
    {
        var extension = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp"
            : throw new InvalidDataException("Unterstützt werden PNG-, JPEG- und WebP-Bilder.");
        var stem = Path.Combine(folder, name);
        var path = stem + extension;
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, token);
            token.ThrowIfCancellationRequested();
            foreach (var previous in new[] { ".jpg", ".jpeg", ".png", ".webp" }.Select(ext => stem + ext).Where(File.Exists))
            {
                if (previous == path) File.Copy(previous, previous + ".bak", true);
                else File.Move(previous, previous + ".bak", true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return path;
    }
}
