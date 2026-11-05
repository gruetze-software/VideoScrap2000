using System.Runtime.InteropServices;
using MediaInfo;
using MediaInfo.Analysis.Native;

namespace MovieScrap2000;

// The packaged Unix libraries use different wchar_t widths. The UTF-8 C API
// avoids that ABI difference; returned buffers remain owned by MediaInfo.
internal sealed class PortableMediaInfoFactory : INativeMediaInfoFactory
{
    public INativeMediaInfo Create() => OperatingSystem.IsWindows()
        ? MediaInfoLibFactory.Instance.Create() : new Utf8MediaInfo();

    private sealed class Utf8MediaInfo : INativeMediaInfo
    {
        private readonly MediaInfo.MediaInfo media;

        public Utf8MediaInfo()
        {
            if (OperatingSystem.IsLinux())
            {
                // The ELF loader does not search beside a dlopened library for
                // its dependencies. Load bundled Zen before MediaInfo itself.
                var zen = Path.Combine(AppContext.BaseDirectory, "libzen.so.0");
                if (!File.Exists(zen)) zen = Path.Combine(AppContext.BaseDirectory,
                    "runtimes", RuntimeInformation.RuntimeIdentifier, "native", "libzen.so.0");
                if (File.Exists(zen)) NativeLibrary.Load(zen);
            }
            media = new MediaInfo.MediaInfo();
        }

        public bool IsAvailable => media.Handle != IntPtr.Zero;
        public string? LibraryVersion => Option("Info_Version");
        public bool Open(string path) => Native.MediaInfoA_Open(media.Handle, path) != IntPtr.Zero;
        public void OpenBufferInit(long size, long offset) => media.OpenBufferInit(size, offset);
        public MediaInfoBufferStatus OpenBufferContinue(ReadOnlySpan<byte> buffer)
        {
            var bytes = buffer.ToArray();
            var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return (MediaInfoBufferStatus)(int)media.OpenBufferContinue(pinned.AddrOfPinnedObject(), (IntPtr)bytes.Length); }
            finally { pinned.Free(); }
        }
        public long OpenBufferContinueGoToGet() => media.OpenBufferContinueGoToGet();
        public void OpenBufferFinalize() => media.OpenBufferFinalize();
        public void Close() => media.Close();
        public void Dispose() => media.Dispose();
        private static string Text(IntPtr pointer) => Marshal.PtrToStringUTF8(pointer) ?? "";
        public string Get(StreamKind kind, int stream, string parameter, InfoKind info, InfoKind search) =>
            Text(Native.MediaInfoA_Get(media.Handle, (IntPtr)kind, (IntPtr)stream, parameter, (IntPtr)info, (IntPtr)search));
        public string Get(StreamKind kind, int stream, int parameter, InfoKind info) =>
            Text(Native.MediaInfoA_GetI(media.Handle, (IntPtr)kind, (IntPtr)stream, (IntPtr)parameter, (IntPtr)info));
        public string Get(StreamKind kind, int stream, string parameter, InfoKind info) => Get(kind, stream, parameter, info, InfoKind.Name);
        public string Get(StreamKind kind, int stream, string parameter) => Get(kind, stream, parameter, InfoKind.Text);
        public string Get(StreamKind kind, int stream, int parameter) => Get(kind, stream, parameter, InfoKind.Text);
        public int CountGet(StreamKind kind, int stream) => media.CountGet(kind, stream);
        public int CountGet(StreamKind kind) => media.CountGet(kind);
        public string Option(string option, string value) => Text(Native.MediaInfoA_Option(media.Handle, option, value));
        public string Option(string option) => Option(option, "");
        public string Inform() => Text(Native.MediaInfoA_Inform(media.Handle, IntPtr.Zero));
    }

    private static class Native
    {
        [DllImport("libmediainfo", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MediaInfoA_Open(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport("libmediainfo", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MediaInfoA_Get(IntPtr handle, IntPtr kind, IntPtr stream,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string parameter, IntPtr info, IntPtr search);
        [DllImport("libmediainfo", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MediaInfoA_GetI(IntPtr handle, IntPtr kind, IntPtr stream, IntPtr parameter, IntPtr info);
        [DllImport("libmediainfo", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MediaInfoA_Option(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string option,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
        [DllImport("libmediainfo", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MediaInfoA_Inform(IntPtr handle, IntPtr reserved);
    }
}
