using System.Security.Cryptography;
using MovieScrap2000;
using SkiaSharp;

namespace VideoScrap2000;

public enum ArtworkProfile { Original, Compact, Balanced }

internal sealed record OptimizedArtwork(byte[] Bytes, int Width, int Height, bool Changed);
internal sealed record ArtworkOptimizationPlan(string Path, string Kind, string Hash, long Before, long After, int Width, int Height);

internal static class ArtworkOptimizer
{
    internal const string OriginalFolder = ".artwork-originals";

    internal static string? Kind(string name)
    {
        if (Path.GetFileName(Path.GetDirectoryName(name)) == ".actors") return "actor";
        var stem = Path.GetFileNameWithoutExtension(name);
        return new[] { "poster", "fanart", "banner", "landscape", "keyart", "thumb", "actor" }
            .FirstOrDefault(kind => stem.Equals(kind, StringComparison.OrdinalIgnoreCase)
                || stem.EndsWith("-" + kind, StringComparison.OrdinalIgnoreCase));
    }

    internal static OptimizedArtwork Optimize(byte[] bytes, string kind, ArtworkProfile profile)
    {
        if (profile == ArtworkProfile.Original || Kind(kind + ".jpg") is null)
            return new(bytes, 0, 0, false);
        using var input = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(input) ?? throw new InvalidDataException("Das Bild konnte nicht gelesen werden.");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 100_000_000)
            throw new InvalidDataException("Das Bild ist zu groß.");
        // Alpha artwork must retain transparency and its original format.
        if (codec.Info.AlphaType != SKAlphaType.Opaque) return new(bytes, codec.Info.Width, codec.Info.Height, false);
        using var srgb = SKColorSpace.CreateSrgb();
        using var decoded = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Opaque, srgb));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            throw new InvalidDataException("Das Bild ist unvollständig oder beschädigt.");
        // Apply all EXIF orientations before calculating the target bounds.
        var origin = (int)codec.EncodedOrigin;
        var swap = origin >= 5;
        var width = swap ? decoded.Height : decoded.Width;
        var height = swap ? decoded.Width : decoded.Height;
        var portrait = kind.Equals("poster", StringComparison.OrdinalIgnoreCase);
        var maxWidth = portrait ? profile == ArtworkProfile.Compact ? 600 : 1000 : profile == ArtworkProfile.Compact ? 1280 : 1920;
        var maxHeight = portrait ? profile == ArtworkProfile.Compact ? 900 : 1500 : profile == ArtworkProfile.Compact ? 720 : 1080;
        var actor = kind.Equals("actor", StringComparison.OrdinalIgnoreCase);
        if (actor)
        {
            maxWidth = profile == ArtworkProfile.Compact ? 300 : 500;
            maxHeight = profile == ArtworkProfile.Compact ? 450 : 750;
        }
        var scale = Math.Min(1d, Math.Min((double)maxWidth / width, (double)maxHeight / height));
        if (actor && scale == 1 && origin == 1) return new(bytes, width, height, false);
        var targetWidth = Math.Max(1, (int)Math.Floor(width * scale));
        var targetHeight = Math.Max(1, (int)Math.Floor(height * scale));
        using var target = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Opaque, srgb));
        using (var canvas = new SKCanvas(target))
        {
            canvas.Scale((float)targetWidth / width, (float)targetHeight / height);
            var matrix = origin switch
            {
                2 => new SKMatrix { ScaleX = -1, ScaleY = 1, TransX = width, Persp2 = 1 },
                3 => new SKMatrix { ScaleX = -1, ScaleY = -1, TransX = width, TransY = height, Persp2 = 1 },
                4 => new SKMatrix { ScaleX = 1, ScaleY = -1, TransY = height, Persp2 = 1 },
                5 => new SKMatrix { SkewX = 1, SkewY = 1, Persp2 = 1 },
                6 => new SKMatrix { SkewX = -1, SkewY = 1, TransX = width, Persp2 = 1 },
                7 => new SKMatrix { SkewX = -1, SkewY = -1, TransX = width, TransY = height, Persp2 = 1 },
                8 => new SKMatrix { SkewX = 1, SkewY = -1, TransY = height, Persp2 = 1 },
                _ => SKMatrix.Identity
            };
            canvas.Concat(matrix);
            using var source = SKImage.FromBitmap(decoded);
            canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
        // Skia's JPEG encoder writes a non-progressive JPEG from decoded sRGB
        // pixels. Original EXIF/XMP/IPTC metadata is not copied to the output.
        using var image = SKImage.FromBitmap(target);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, profile == ArtworkProfile.Compact ? 85 : 88)
            ?? throw new InvalidDataException("Das Bild konnte nicht gespeichert werden.");
        return new(data.ToArray(), targetWidth, targetHeight, true);
    }

    internal static async Task<string> SaveAsync(string folder, string stem, byte[] bytes, string kind, ArtworkProfile profile, CancellationToken token)
    {
        var optimized = Optimize(bytes, kind, profile);
        token.ThrowIfCancellationRequested();
        if (optimized.Changed)
        {
            // Keep every distinct original, including both the current on-disk
            // artwork and a replacement download. Repeated runs reuse backups.
            var originals = Path.Combine(folder, OriginalFolder);
            Directory.CreateDirectory(originals);
            async Task PreserveAsync(byte[] sourceBytes)
            {
                var extension = sourceBytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) ? ".jpg"
                    : sourceBytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71 }) ? ".png" : ".webp";
                var hash = Convert.ToHexString(SHA256.HashData(sourceBytes));
                var backup = Path.Combine(originals, stem + "." + hash + extension);
                if (File.Exists(backup)) return;
                var temporary = backup + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    await File.WriteAllBytesAsync(temporary, sourceBytes, token);
                    token.ThrowIfCancellationRequested();
                    File.Move(temporary, backup, overwrite: false);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".webp" })
                if (File.Exists(Path.Combine(folder, stem + extension)))
                    await PreserveAsync(await File.ReadAllBytesAsync(Path.Combine(folder, stem + extension), token));
            await PreserveAsync(bytes);
        }
        return await ArtworkFiles.SaveNamedAsync(folder, stem, optimized.Bytes, token);
    }

    internal static async Task SaveMovieImageAsync(string folder, string videoName, ImageCacheItem image, ArtworkProfile profile, CancellationToken token)
    {
        var kind = image.Type.ToLowerInvariant();
        if (profile == ArtworkProfile.Original || Kind(kind + ".jpg") is null)
        {
            await ArtworkFiles.SaveAsync(folder, videoName, image, token);
            return;
        }
        await SaveAsync(folder, videoName + "-" + kind, image.Data, kind, profile, token);
    }

    internal static async Task<List<ArtworkOptimizationPlan>> AnalyzeAsync(string folder, ArtworkProfile profile, Action<string> progress, CancellationToken token)
    {
        if (profile == ArtworkProfile.Original) return [];
        var result = new List<ArtworkOptimizationPlan>();
        var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false };
        foreach (var path in Directory.EnumerateFiles(folder, "*", options))
        {
            token.ThrowIfCancellationRequested();
            if (Path.GetRelativePath(folder, path).Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith('.') && part != ".actors")) continue;
            if (Path.GetExtension(path).ToLowerInvariant() is not (".jpg" or ".jpeg" or ".png" or ".webp") || Kind(path) is not { } kind) continue;
            progress(path);
            var bytes = await File.ReadAllBytesAsync(path, token);
            var optimized = Optimize(bytes, kind, profile);
            if (optimized.Changed)
                result.Add(new(path, kind, Convert.ToHexString(SHA256.HashData(bytes)), bytes.Length, optimized.Bytes.Length, optimized.Width, optimized.Height));
        }
        return result;
    }

    internal static async Task ApplyAsync(ArtworkOptimizationPlan plan, ArtworkProfile profile, CancellationToken token)
    {
        var bytes = await File.ReadAllBytesAsync(plan.Path, token);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != plan.Hash)
            throw new IOException("Das Bild wurde seit der Vorschau verändert. Bitte erneut prüfen.");
        await SaveAsync(Path.GetDirectoryName(plan.Path)!, Path.GetFileNameWithoutExtension(plan.Path), bytes, plan.Kind, profile, token);
    }

    internal static async Task OptimizeActorPortraitsAsync(string folder, IReadOnlyList<Actor> actors, ArtworkProfile profile, CancellationToken token)
    {
        if (profile == ArtworkProfile.Original) return;
        var actorFolder = Path.Combine(folder, ".actors");
        bool NeedsOptimization(byte[] bytes)
        {
            try { return Optimize(bytes, "actor", profile).Changed; }
            catch (InvalidDataException ex) { System.Diagnostics.Trace.WriteLine(ex); return false; }
        }
        var local = Directory.Exists(actorFolder) ? Directory.GetFiles(actorFolder)
            .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp").ToArray() : [];
        var renamed = new Dictionary<string, string>(LibrarySources.PathComparer);
        foreach (var path in local)
        {
            token.ThrowIfCancellationRequested();
            var bytes = await File.ReadAllBytesAsync(path, token);
            if (!NeedsOptimization(bytes)) continue;
            renamed[path] = await SaveAsync(actorFolder, Path.GetFileNameWithoutExtension(path), bytes, "actor", profile, token);
        }
        foreach (var actor in actors)
        {
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(actor.Thumb) && !Uri.IsWellFormedUriString(actor.Thumb, UriKind.Absolute)
                && renamed.TryGetValue(Path.GetFullPath(Path.Combine(folder, actor.Thumb)), out var replacement))
                actor.Thumb = Path.GetRelativePath(folder, replacement).Replace('\\', '/');
            if (actor.ThumbData is not { Length: > 0 } data || !NeedsOptimization(data) || string.IsNullOrWhiteSpace(actor.Name)) continue;
            var stem = string.Concat(actor.Name.Select(character => char.IsControl(character) || "<>:\"/\\|?*".Contains(character) ? '_' : character)).Trim().TrimEnd('.').Replace(' ', '_');
            if (stem.Length == 0) continue;
            Directory.CreateDirectory(actorFolder);
            var path = await SaveAsync(actorFolder, stem, data, "actor", profile, token);
            actor.Thumb = Path.GetRelativePath(folder, path).Replace('\\', '/');
            actor.ThumbData = await File.ReadAllBytesAsync(path, token);
        }
    }
}
