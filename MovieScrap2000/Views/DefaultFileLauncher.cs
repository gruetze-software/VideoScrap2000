using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace VideoScrap2000.Views;

internal static class DefaultFileLauncher
{
    public static async Task<bool> LaunchAsync(TopLevel owner, IStorageItem file)
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/.flatpak-info"))
            return await owner.Launcher.LaunchFileAsync(file);

        var path = file.TryGetLocalPath();
        if (path is null) return false;
        // GIO exports local files through OpenURI.OpenFile in Flatpak. Passing
        // a sandbox-local file URI to a host process cannot do that reliably.
        var start = CreatePortalStartInfo(path);
        using var process = Process.Start(start);
        if (process is null) return false;
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill();
            throw new IOException("The desktop did not respond to the file-open request.");
        }
        var details = await error;
        if (process.ExitCode != 0)
            throw new IOException(string.IsNullOrWhiteSpace(details) ? "The desktop could not open the file." : details.Trim());
        return true;
    }

    internal static ProcessStartInfo CreatePortalStartInfo(string path)
    {
        var start = new ProcessStartInfo("gio")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("open");
        start.ArgumentList.Add(new Uri(Path.GetFullPath(path)).AbsoluteUri);
        start.Environment["GIO_USE_PORTALS"] = "1";
        return start;
    }
}
