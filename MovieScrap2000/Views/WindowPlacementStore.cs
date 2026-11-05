using System.Text.Json;
using MovieScrap2000;

namespace VideoScrap2000.Views;

public sealed record WindowPlacement(double Width = 1280, double Height = 860, bool Maximized = false)
{
    public WindowPlacement Validated() => this with
    {
        Width = double.IsFinite(Width) && Width > 0 ? Math.Min(Width, 16384) : 1280,
        Height = double.IsFinite(Height) && Height > 0 ? Math.Min(Height, 16384) : 860
    };
}

public sealed class WindowPlacementStore(string path)
{
    internal static WindowPlacementStore ForMode(bool demo) => new(Path.Combine(Utils.GetOptionsPath(),
        demo ? Path.Combine("Demo", "window.json") : "window.json"));

    public WindowPlacement Load()
    {
        try { return (JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(path)) ?? new()).Validated(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public void Save(WindowPlacement placement)
    {
        var destination = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(placement.Validated()));
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
