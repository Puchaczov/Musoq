namespace Musoq.Evaluator.Visitors;

internal static class SchemaIntrinsicCodeEmitter
{
    internal static string? ResolveName(string name) => name.ToUpperInvariant() switch
    {
        "LENGTH" => "Length", "CRC32" => "Crc32", "CRC16" => "Crc16", "ADLER32" => "Adler32",
        "MD5" => "Md5", "SHA256" => "Sha256", "SUBSTRING" => "Substring", "INDEXOF" => "IndexOf",
        "TOHEX" => "ToHex", "FROMHEX" => "FromHex", "TOTEXT" => "ToText", _ => null
    };

    internal static string Emit(string name) => "internal static class " + name + "\n" +
        """
        {
            internal static T Invoke<T>(Func<T> operation, Func<ParseErrorCode, Exception, ParseException> failure)
            {
                try { return operation(); }
                catch (System.Text.DecoderFallbackException error)
                {
                    throw failure(ParseErrorCode.EncodingError, error);
                }
                catch (Exception error) when (error is ArgumentException or ArithmeticException or FormatException or InvalidCastException)
                {
                    throw failure(ParseErrorCode.FieldReferenceError, error);
                }
            }

            private static T Require<T>(T value) where T : class =>
                value ?? throw new ArgumentNullException(nameof(value));

            internal static int Length(byte[] value) => Require(value).Length;
            private static int RequireInteger(int? value) => value ?? throw new ArgumentNullException(nameof(value));
            internal static string Substring(string value, int? start, int? length) =>
                Require(value).Substring(RequireInteger(start), RequireInteger(length));
            internal static string ToHex(byte[] value) => Convert.ToHexString(Require(value));
            internal static byte[] FromHex(string value) => Convert.FromHexString(Require(value));
            internal static byte[] Md5(byte[] value) => System.Security.Cryptography.MD5.HashData(Require(value));
            internal static byte[] Sha256(byte[] value) => System.Security.Cryptography.SHA256.HashData(Require(value));
            internal static int IndexOf(byte[] value, byte[] pattern) =>
                System.MemoryExtensions.IndexOf<byte>(Require(value).AsSpan(), Require(pattern).AsSpan());

            internal static string ToText(byte[] value, string encoding)
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var codePage = Require(encoding).Replace("-", string.Empty).ToLowerInvariant() switch
                {
                    "utf8" => 65001, "utf16le" => 1200, "utf16be" => 1201,
                    "ascii" => 20127, "latin1" => 28591, "ebcdic" => 37,
                    _ => throw new ArgumentException("Unknown schema encoding.", nameof(encoding))
                };
                var codec = System.Text.Encoding.GetEncoding(codePage,
                    System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);
                return codec.GetString(Require(value));
            }

            private static uint ComputeCrc(byte[] value, uint initial, uint polynomial)
            {
                var crc = initial;
                foreach (var item in Require(value))
                {
                    crc ^= item;
                    for (var bit = 0; bit < 8; bit++)
                        crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
                }
                return crc;
            }

            internal static int Crc32(byte[] value) => unchecked((int)~ComputeCrc(value, 0xFFFFFFFF, 0xEDB88320));
            internal static short Crc16(byte[] value) => unchecked((short)ComputeCrc(value, 0, 0xA001));

            internal static int Adler32(byte[] value)
            {
                uint a = 1;
                uint b = 0;
                foreach (var item in Require(value))
                {
                    a = (a + item) % 65521;
                    b = (b + a) % 65521;
                }
                return unchecked((int)((b << 16) | a));
            }
        }
        """;
}
