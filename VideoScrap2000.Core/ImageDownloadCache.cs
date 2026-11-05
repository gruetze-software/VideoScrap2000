namespace MovieScrap2000;

internal static class ImageDownloadCache
{
    private const long Capacity = 32 * 1024 * 1024;
    private static readonly object Sync = new();
    private static readonly Dictionary<string, LinkedListNode<(string Url, byte[] Data)>> Entries = [];
    private static readonly LinkedList<(string Url, byte[] Data)> Recent = [];
    private static long size;

    internal static bool TryGet(string url, out byte[]? data)
    {
        lock (Sync)
        {
            data = null;
            if (!Entries.TryGetValue(url, out var entry)) return false;
            Recent.Remove(entry);
            Recent.AddFirst(entry);
            data = entry.Value.Data;
            return true;
        }
    }

    internal static void Store(string url, byte[] data)
    {
        if (data.Length == 0 || data.LongLength > Capacity) return;
        lock (Sync)
        {
            if (Entries.Remove(url, out var previous))
            {
                Recent.Remove(previous);
                size -= previous.Value.Data.LongLength;
            }
            Entries[url] = Recent.AddFirst((url, data));
            size += data.LongLength;
            while ((size > Capacity || Entries.Count > 256) && Recent.Last is { } oldest)
            {
                Entries.Remove(oldest.Value.Url);
                Recent.RemoveLast();
                size -= oldest.Value.Data.LongLength;
            }
        }
    }
}
