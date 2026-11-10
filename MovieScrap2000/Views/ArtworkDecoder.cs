using Avalonia.Media.Imaging;
using SkiaSharp;

namespace VideoScrap2000.Views;

internal static class ArtworkDecoder
{
    public static Avalonia.PixelSize SourceSize(byte[] bytes)
    {
        using var input = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(input);
        if (codec is null) return default;
        return (int)codec.EncodedOrigin >= 5 ? new(codec.Info.Height, codec.Info.Width) : new(codec.Info.Width, codec.Info.Height);
    }
    public static Bitmap? DecodePreview(byte[] bytes, int width)
    {
        // An unsupported response has no codec. Check before calling SKBitmap.Decode.
        using var input = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(input);
        if (codec is null) return null;

        var scale = Math.Min(1f, (float)width / codec.Info.Width);
        var dimensions = codec.GetScaledDimensions(scale);
        using var decoded = SKBitmap.Decode(codec, new SKImageInfo(dimensions.Width, dimensions.Height,
            SKColorType.Bgra8888, SKAlphaType.Premul));
        if (decoded is null) return null;
        using var image = SKImage.FromBitmap(decoded);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        if (png is null) return null;
        using var stream = png.AsStream();
        return Bitmap.DecodeToWidth(stream, width);
    }
}
