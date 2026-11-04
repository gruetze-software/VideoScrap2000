namespace MovieScrap2000;

public static class ArtworkFiles
{
    public static async Task SaveAsync(string folder, string videoName, ImageCacheItem image, CancellationToken token)
    {
        var suffix = image.Type switch
        {
            ImageTypes.Poster => "poster", ImageTypes.Fanart => "fanart", ImageTypes.ClearLogo => "logo",
            ImageTypes.ClearArt => "clearart", ImageTypes.Banner => "banner", ImageTypes.Landscape => "landscape",
            ImageTypes.KeyArt => "keyart", ImageTypes.DiskArt => "discart", _ => throw new ArgumentException("Unbekannter Bildtyp.")
        };
        var bytes = image.Data;
        var extension = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp"
            : throw new InvalidDataException("Unterstützt werden PNG-, JPEG- und WebP-Bilder.");
        var stem = Path.Combine(folder, videoName + "-" + suffix);
        var path = stem + extension;
        var temporary = path + ".tmp";
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
    }
}
