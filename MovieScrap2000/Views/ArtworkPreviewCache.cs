using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Avalonia.Media.Imaging;
using VideoScrap2000.ViewModels;

namespace VideoScrap2000.Views;

internal sealed class ArtworkPreviewCache(long capacity = 96 * 1024 * 1024)
{
    internal static ArtworkPreviewCache Shared { get; } = new();
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly ConditionalWeakTable<byte[], Digest> Digests = new();
    private readonly object sync = new();
    private readonly Dictionary<string, Entry> entries = [];
    private readonly LinkedList<Entry> recent = [];
    private readonly Dictionary<string, RequestGate> requests = [];
    private readonly SemaphoreSlim loadSlots = new(4);
    private readonly Dictionary<string, ArtworkPaths> paths = [];
    private long size;
    private long revision;
    private static readonly TimeSpan RecheckAfter = TimeSpan.FromSeconds(30);

    internal async Task<Lease?> AcquireAsync(string path, int width, byte[]? data = null,
        CancellationToken token = default, bool retainOriginal = true)
    {
        token.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        var memoryKey = data is null ? "" : Digests.GetValue(data,
            bytes => new(Convert.ToHexString(SHA256.HashData(bytes)))).Value;
        var remote = Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
        var source = OperatingSystem.IsWindows() && !remote
            ? path.ToUpperInvariant() : path;
        var key = $"{source}|{width}|{memoryKey}|{retainOriginal}";
        var local = data is null && !remote;
        lock (sync)
            if (TryAcquire(key, local, out var cached)) return cached;

        RequestGate gate;
        lock (sync)
        {
            if (!requests.TryGetValue(key, out gate!)) requests[key] = gate = new();
            gate.Users++;
        }
        var entered = false;
        try
        {
            await gate.Semaphore.WaitAsync(token);
            entered = true;
            lock (sync)
                if (TryAcquire(key, local, out var cached)) return cached;
            await loadSlots.WaitAsync(token);
            try
            {
                return await Task.Run(async () =>
                {
                    var stamp = local ? FileStamp.Read(path) : default;
                    long startedAt;
                    lock (sync)
                    {
                        startedAt = revision;
                        if (entries.TryGetValue(key, out var existing) && existing.Stamp == stamp)
                        {
                            existing.CheckedAt = DateTime.UtcNow;
                            return Acquire(existing);
                        }
                    }
                    var bytes = data ?? (remote
                        ? await Client.GetByteArrayAsync(path, token)
                        : await File.ReadAllBytesAsync(path, token));
                    var bitmap = ArtworkDecoder.DecodePreview(bytes, width);
                    if (bitmap is null) return null;
                    if (token.IsCancellationRequested) { bitmap.Dispose(); token.ThrowIfCancellationRequested(); }
                    var entry = new Entry(key, path, bitmap, local || !retainOriginal ? null : bytes, stamp, ArtworkDecoder.SourceSize(bytes));
                    lock (sync)
                    {
                        var lease = Acquire(entry);
                        if (startedAt == revision)
                        {
                            if (entries.TryGetValue(key, out var previous)) Remove(previous);
                            entry.Node = recent.AddFirst(entry);
                            entries[key] = entry;
                            size += entry.Size;
                            while ((size > capacity || entries.Count > 1024) && recent.Last is { } last) Remove(last.Value);
                        }
                        return lease;
                    }
                }, token);
            }
            finally { loadSlots.Release(); }
        }
        finally
        {
            if (entered) gate.Semaphore.Release();
            lock (sync)
            {
                if (--gate.Users == 0) { requests.Remove(key); gate.Semaphore.Dispose(); }
            }
        }
    }

    internal async Task<IReadOnlyList<(string Type, string Path)>> FindArtworkAsync(string folder, string video,
        CancellationToken token)
    {
        var key = Path.Combine(folder, Path.GetFileName(video));
        long startedAt;
        lock (sync)
        {
            if (paths.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.CheckedAt < RecheckAfter)
                return cached.Items;
            startedAt = revision;
        }
        var result = await Task.Run(() => MainViewModel.FindArtwork(folder, video).ToArray(), token);
        lock (sync)
        {
            if (startedAt != revision) return result;
            if (paths.Count >= 512) paths.Remove(paths.MinBy(item => item.Value.CheckedAt).Key);
            paths[key] = new(folder, result, DateTime.UtcNow);
        }
        return result;
    }

    internal void InvalidateFolder(string folder)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        lock (sync)
        {
            revision++;
            foreach (var entry in entries.Values.Where(entry =>
                string.Equals(Path.GetDirectoryName(entry.Path), folder, comparison)).ToArray()) Remove(entry);
            foreach (var key in paths.Where(item => string.Equals(item.Value.Folder, folder, comparison))
                .Select(item => item.Key).ToArray()) paths.Remove(key);
        }
    }

    internal void RefreshFileState()
    {
        lock (sync)
        {
            revision++;
            paths.Clear();
            foreach (var entry in entries.Values.Where(entry => entry.Data is null))
                entry.CheckedAt = DateTime.MinValue;
        }
    }

    private bool TryAcquire(string key, bool local, out Lease? lease)
    {
        lease = null;
        if (!entries.TryGetValue(key, out var entry)
            || local && DateTime.UtcNow - entry.CheckedAt >= RecheckAfter) return false;
        lease = Acquire(entry);
        return true;
    }

    private Lease Acquire(Entry entry)
    {
        entry.Users++;
        if (entry.Node is { } node) { recent.Remove(node); recent.AddFirst(node); }
        return new(this, entry);
    }

    private void Remove(Entry entry)
    {
        entries.Remove(entry.Key);
        recent.Remove(entry.Node!);
        entry.Node = null;
        size -= entry.Size;
        if (entry.Users == 0) entry.Bitmap.Dispose();
    }

    private void Release(Entry entry)
    {
        lock (sync)
        {
            entry.Users--;
            if (entry.Users == 0 && entry.Node is null) entry.Bitmap.Dispose();
        }
    }

    internal sealed class Lease : IDisposable
    {
        private readonly ArtworkPreviewCache owner;
        private Entry? entry;
        internal Lease(ArtworkPreviewCache owner, Entry entry) { this.owner = owner; this.entry = entry; }
        internal Bitmap Image => entry!.Bitmap;
        internal string Path => entry!.Path;
        internal byte[]? OriginalData => entry!.Data;
        internal string Resolution => entry is { } image && image.SourceSize.Width > 0 ? $"{image.SourceSize.Width} × {image.SourceSize.Height}" : "";
        public void Dispose()
        {
            if (Interlocked.Exchange(ref entry, null) is { } previous) owner.Release(previous);
        }
    }

    internal sealed class Entry(string key, string path, Bitmap bitmap, byte[]? data, FileStamp stamp, Avalonia.PixelSize sourceSize)
    {
        internal Avalonia.PixelSize SourceSize { get; } = sourceSize;
        internal string Key { get; } = key;
        internal string Path { get; } = path;
        internal Bitmap Bitmap { get; } = bitmap;
        internal byte[]? Data { get; } = data;
        internal FileStamp Stamp { get; } = stamp;
        internal long Size { get; } = (long)bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4 + (data?.LongLength ?? 0);
        internal DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        internal int Users { get; set; }
        internal LinkedListNode<Entry>? Node { get; set; }
    }

    internal readonly record struct FileStamp(long Length, long Modified)
    {
        internal static FileStamp Read(string path)
        {
            var file = new FileInfo(path);
            return new(file.Length, file.LastWriteTimeUtc.Ticks);
        }
    }

    private sealed record Digest(string Value);
    private sealed class RequestGate
    {
        internal SemaphoreSlim Semaphore { get; } = new(1);
        internal int Users { get; set; }
    }
    private sealed record ArtworkPaths(string Folder, IReadOnlyList<(string Type, string Path)> Items, DateTime CheckedAt);
}
