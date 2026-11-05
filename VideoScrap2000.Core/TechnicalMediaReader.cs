using System.Globalization;
using MediaInfo.Analysis;
using MediaInfo.Analysis.Results;

namespace MovieScrap2000;

public static class TechnicalMediaReader
{
    private static readonly IMediaInfoAnalyzer Analyzer = MediaInfoAnalyzerBuilder.Create()
        .WithValidation().WithConcurrencyLimit(2)
        .WithTimeout(TimeSpan.FromSeconds(20)).Build();

    public static bool HasVideoDetails(MovieFileInfo? info) =>
        info?.StreamDetails?.VideoStream is { Width: > 0, Height: > 0, Codec.Length: > 0 };

    public static async Task<MovieFileInfo?> ReadAsync(string path, CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 65536, FileOptions.Asynchronous);
            var result = await Analyzer.AnalyzeAsync(stream, leaveOpen: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (!result.Success)
            {
                System.Diagnostics.Trace.WriteLine(result.Failure?.Message);
                return null;
            }
            return Convert(result);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            return null;
        }
    }

    private static MovieFileInfo? Convert(MediaAnalysisResult result)
    {
        if (result.BestVideoStream is not { Width: > 0, Height: > 0 } video) return null;
        var duration = video.Duration > TimeSpan.Zero ? video.Duration : result.General.Duration;
        return new()
        {
            StreamDetails = new()
            {
                VideoStream = new()
                {
                    Codec = VideoCodec(video.Format, video.CodecName),
                    Width = video.Width,
                    Height = video.Height,
                    Aspect = Aspect(video.DisplayAspectRatio, video.Width, video.Height),
                    DurationInSeconds = (int)Math.Clamp(Math.Round(duration.TotalSeconds), 0, int.MaxValue),
                    Bitrate = (decimal)video.Bitrate,
                    FileSize = result.General.Size,
                    ScanType = video.ScanType
                },
                AudioStreams = result.AudioStreams.Select(audio => new AudioStream
                {
                    Codec = AudioCodec(audio.Format, audio.CodecName),
                    Channels = audio.Channel,
                    Language = LanguageCode(audio.LanguageIetf, audio.Language),
                    LongLanguage = audio.Language,
                    Bitrate = (decimal)audio.Bitrate
                }).ToList(),
                Subtitles = result.Subtitles.Select(subtitle => new SubtitleStream
                    { Language = LanguageCode(subtitle.LanguageIetf, subtitle.Language) }).ToList()
            }
        };
    }

    private static string? LanguageCode(string? ietf, string? name)
    {
        if (!string.IsNullOrWhiteSpace(ietf))
        {
            try { return CultureInfo.GetCultureInfo(ietf).ThreeLetterISOLanguageName; }
            catch (CultureNotFoundException) { }
        }
        return CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .FirstOrDefault(culture => culture.EnglishName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.ThreeLetterISOLanguageName ?? name;
    }

    private static decimal Aspect(string? ratio, int width, int height)
    {
        var parts = ratio?.Split(':') ?? [];
        if (parts.Length == 2
            && decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
            && decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
            && numerator > 0 && denominator > 0) return numerator / denominator;
        return decimal.TryParse(ratio, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value : (decimal)width / height;
    }

    private static string VideoCodec(string? format, string? name) => format?.ToUpperInvariant() switch
    {
        "AVC" => "h264",
        "HEVC" => "hevc",
        "MPEG-4 VISUAL" => "mpeg4",
        "MPEG VIDEO" => "mpeg2video",
        "VC-1" => "vc1",
        _ => (name ?? format ?? "").ToLowerInvariant()
    };

    private static string AudioCodec(string? format, string? name) => format?.ToUpperInvariant() switch
    {
        "AC-3" => "ac3",
        "E-AC-3" => "eac3",
        "MLP FBA" => "truehd",
        "DTS" => "dts",
        "AAC" or "AAC LC" => "aac",
        "MPEG AUDIO" => "mp3",
        _ => (name ?? format ?? "").ToLowerInvariant()
    };
}
