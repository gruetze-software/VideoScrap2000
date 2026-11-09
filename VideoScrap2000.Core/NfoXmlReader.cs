using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MovieScrap2000;

internal static class NfoXmlReader
{
    internal static XmlReader Open(string path)
    {
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        try
        {
            var buffer = new byte[512];
            var length = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            stream.Position = 0;
            ReadOnlySpan<byte> prefix = buffer.AsSpan(0, length);
            if (prefix.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) prefix = prefix[3..];
            var declarationEnd = prefix.IndexOf("?>"u8);
            var legacyUtf8 = prefix.StartsWith("<?xml"u8) && declarationEnd >= 0
                && Regex.IsMatch(Encoding.ASCII.GetString(prefix[..(declarationEnd + 2)]),
                    @"\bencoding\s*=\s*(['""])utf-16(?:le|be)?\1", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var settings = new XmlReaderSettings { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            // Earlier versions wrote UTF-8 bytes with a StringWriter's UTF-16
            // declaration. Detect that byte/header mismatch before XML parsing,
            // avoiding a first-chance exception for every NFO in the debugger.
            // Real UTF-16/32 and other declared encodings keep normal detection.
            return legacyUtf8
                ? XmlReader.Create(new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true), settings)
                : XmlReader.Create(stream, settings);
        }
        catch { stream.Dispose(); throw; }
    }
}
