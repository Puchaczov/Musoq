using System.Text;

namespace Musoq.Schema.Interpreters;

internal static class BinaryStringEncodings
{
    private static readonly Encoding Utf8 = Prepare(Encoding.UTF8);
    private static readonly Encoding Utf16Le = Prepare(Encoding.Unicode);
    private static readonly Encoding Utf16Be = Prepare(Encoding.BigEndianUnicode);
    private static readonly Encoding Ascii = Prepare(Encoding.ASCII);
    private static readonly Encoding Latin1 = Prepare(Encoding.Latin1);

    internal static Encoding StrictDecoder(Encoding encoding)
    {
        if (ReferenceEquals(encoding, Encoding.UTF8))
            return Utf8;
        if (ReferenceEquals(encoding, Encoding.Unicode))
            return Utf16Le;
        if (ReferenceEquals(encoding, Encoding.BigEndianUnicode))
            return Utf16Be;
        if (ReferenceEquals(encoding, Encoding.ASCII))
            return Ascii;
        if (ReferenceEquals(encoding, Encoding.Latin1))
            return Latin1;
        if (ReferenceEquals(encoding, Ebcdic.Source))
            return Ebcdic.Strict!;

        // A caller-supplied encoding can customize cloning and decoding even for a standard code page.
        return Prepare(encoding);
    }

    private static Encoding Prepare(Encoding encoding)
    {
        var strict = (Encoding)encoding.Clone();
        strict.DecoderFallback = DecoderFallback.ExceptionFallback;
        return strict;
    }

    private static class Ebcdic
    {
        internal static readonly Encoding? Source = CodePagesEncodingProvider.Instance.GetEncoding(37);
        internal static readonly Encoding? Strict = Source is null ? null : Prepare(Source);
    }
}
