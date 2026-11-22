using Avalonia.Platform.Storage;
using MovieScrap2000;
using System.Text.Json;

namespace VideoScrap2000.Views;

internal static class MacFolderAccess
{
    private static readonly List<IStorageFolder> OpenFolders = [];
    private static readonly Dictionary<string, string> Bookmarks = [];
    private static string FilePath => Path.Combine(Utils.GetOptionsPath(), "folder-bookmarks.json");

    internal static async Task RestoreAsync(IStorageProvider storage, IEnumerable<string> configuredFolders)
    {
        if (!ReleaseUpdateCheck.IsMacAppStore) return;
        if (File.Exists(FilePath))
            foreach (var entry in JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(FilePath)) ?? [])
                Bookmarks[entry.Key] = entry.Value;
        var errors = new List<string>();
        foreach (var path in configuredFolders.Distinct(StringComparer.Ordinal))
        {
            try
            {
                if (!Bookmarks.TryGetValue(path, out var bookmark) ||
                    await storage.OpenFolderBookmarkAsync(bookmark) is not { } folder)
                {
                    errors.Add(path);
                    continue;
                }
                OpenFolders.Add(folder);
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine(ex); errors.Add(path); }
        }
        if (errors.Count > 0)
            throw new IOException(Localization.Locale.T("Bitte diese Ordner in den Einstellungen erneut auswählen:") +
                Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    internal static async Task RememberAsync(IStorageFolder folder)
    {
        if (!ReleaseUpdateCheck.IsMacAppStore || folder.TryGetLocalPath() is not { } path) return;
        var bookmark = await folder.SaveBookmarkAsync();
        if (string.IsNullOrEmpty(bookmark))
            throw new IOException(Localization.Locale.T("Der Ordnerzugriff konnte nicht dauerhaft gespeichert werden."));
        Bookmarks[path] = bookmark;
        OpenFolders.Add(folder);
        var temporary = FilePath + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(Bookmarks));
        if (OperatingSystem.IsMacOS())
            File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, FilePath, true);
    }

    internal static void Release()
    {
        foreach (var folder in OpenFolders) folder.Dispose();
        OpenFolders.Clear();
        Bookmarks.Clear();
    }
}
