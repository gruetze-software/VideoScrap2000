using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace VideoScrap2000.Views;

internal static class ReleaseUpdateCheck
{
    private const string Repository = "https://github.com/gruetze-software/VideoScrap2000";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    internal static bool IsAvailable => OperatingSystem.IsWindows() ? !HasWindowsPackageIdentity()
        : !(OperatingSystem.IsLinux() && (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNAP")) ||
            (File.Exists("/.flatpak-info") && Environment.GetEnvironmentVariable("VIDEOSCRAP2000_GITHUB_BUNDLE") != "1")));

    internal static async Task<Version> GetLatestVersionAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/gruetze-software/VideoScrap2000/releases/latest");
        request.Headers.UserAgent.ParseAdd("VideoScrap2000");
        using var response = await Client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        var release = await response.Content.ReadFromJsonAsync<Release>(cancellationToken: token);
        if (release is { Draft: false, Prerelease: false } && Version.TryParse(release.Tag?.TrimStart('v'), out var version) &&
            version.Build >= 0 && version.Revision < 0)
            return version;
        throw new InvalidDataException("Unexpected GitHub release version.");
    }

    internal static string DownloadUrl(Version version) => $"{Repository}/releases/tag/v{version}#downloads";

    private static bool HasWindowsPackageIdentity()
    {
        uint length = 0;
        return GetCurrentPackageFullName(ref length, IntPtr.Zero) == 122; // ERROR_INSUFFICIENT_BUFFER
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern int GetCurrentPackageFullName(ref uint length, IntPtr packageFullName);

    private sealed record Release([property: JsonPropertyName("tag_name")] string? Tag, bool Draft, bool Prerelease);
}
