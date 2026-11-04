using Avalonia.Media.Imaging;
using SkiaSharp;

namespace VideoScrap2000.Views;

internal static class ArtworkDecoder
{
    public static Bitmap? DecodePreview(byte[] bytes, int width)
    {
        // An unsupported response has no codec. Check before calling SKBitmap.Decode.
        using var input = new MemoryStream(bytes, writable: false);
        using var codec = SKCodec.Create(input);
        if (codec is null) return null;

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) return null;
        using var image = SKImage.FromBitmap(decoded);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        if (png is null) return null;
        using var stream = png.AsStream();
        return Bitmap.DecodeToWidth(stream, width);
    }
}
