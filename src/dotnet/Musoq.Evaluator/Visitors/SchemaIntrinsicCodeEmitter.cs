namespace Musoq.Evaluator.Visitors;

internal static class SchemaIntrinsicCodeEmitter
{
    internal static string? ResolveName(string name) =>
        name.ToUpperInvariant() switch
        {
            "LENGTH" => "Length",
            "CRC32" => "Crc32",
            "CRC16" => "Crc16",
            "ADLER32" => "Adler32",
            "MD5" => "Md5",
            "SHA256" => "Sha256",
            "SUBSTRING" => "Substring",
            "INDEXOF" => "IndexOf",
            "TOHEX" => "ToHex",
            "FROMHEX" => "FromHex",
            "TOTEXT" => "ToText",
            _ => null,
        };

    internal static string Emit(string name) =>
        "internal static class "
        + name
        + "\n"
        + (
            "{\n"
            + "    internal static T Invoke<T>(Func<T> operation, Func<ParseErrorCode, Exception, ParseException> failure)\n"
            + "    {\n"
            + "        try { return operation(); }\n"
            + "        catch (System.Text.DecoderFallbackException error)\n"
            + "        {\n"
            + "            throw failure(ParseErrorCode.EncodingError, error);\n"
            + "        }\n"
            + "        catch (Exception error) when (error is ArgumentException or ArithmeticException or FormatException or InvalidCastException)\n"
            + "        {\n"
            + "            throw failure(ParseErrorCode.FieldReferenceError, error);\n"
            + "        }\n"
            + "    }\n"
            + "\n"
            + "    private static T Require<T>(T value) where T : class =>\n"
            + "        value ?? throw new ArgumentNullException(nameof(value));\n"
            + "\n"
            + "    internal static int Length(byte[] value) => Require(value).Length;\n"
            + "    private static int RequireInteger(int? value) => value ?? throw new ArgumentNullException(nameof(value));\n"
            + "    internal static string Substring(string value, int? start, int? length) =>\n"
            + "        Require(value).Substring(RequireInteger(start), RequireInteger(length));\n"
            + "    internal static string ToHex(byte[] value) => Convert.ToHexString(Require(value));\n"
            + "    internal static byte[] FromHex(string value) => Convert.FromHexString(Require(value));\n"
            + "    internal static byte[] Md5(byte[] value) => System.Security.Cryptography.MD5.HashData(Require(value));\n"
            + "    internal static byte[] Sha256(byte[] value) => System.Security.Cryptography.SHA256.HashData(Require(value));\n"
            + "    internal static int IndexOf(byte[] value, byte[] pattern) =>\n"
            + "        System.MemoryExtensions.IndexOf<byte>(Require(value).AsSpan(), Require(pattern).AsSpan());\n"
            + "\n"
            + "    internal static string ToText(byte[] value, string encoding)\n"
            + "    {\n"
            + "        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);\n"
            + "        var codePage = Require(encoding).Replace(\"-\", string.Empty).ToLowerInvariant() switch\n"
            + "        {\n"
            + "            \"utf8\" => 65001, \"utf16le\" => 1200, \"utf16be\" => 1201,\n"
            + "            \"ascii\" => 20127, \"latin1\" => 28591, \"ebcdic\" => 37,\n"
            + "            _ => throw new ArgumentException(\"Unknown schema encoding.\", nameof(encoding))\n"
            + "        };\n"
            + "        var codec = System.Text.Encoding.GetEncoding(codePage,\n"
            + "            System.Text.EncoderFallback.ExceptionFallback, System.Text.DecoderFallback.ExceptionFallback);\n"
            + "        return codec.GetString(Require(value));\n"
            + "    }\n"
            + "\n"
            + "    private static uint ComputeCrc(byte[] value, uint initial, uint polynomial)\n"
            + "    {\n"
            + "        var crc = initial;\n"
            + "        foreach (var item in Require(value))\n"
            + "        {\n"
            + "            crc ^= item;\n"
            + "            for (var bit = 0; bit < 8; bit++)\n"
            + "                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;\n"
            + "        }\n"
            + "        return crc;\n"
            + "    }\n"
            + "\n"
            + "    internal static int Crc32(byte[] value) => unchecked((int)~ComputeCrc(value, 0xFFFFFFFF, 0xEDB88320));\n"
            + "    internal static short Crc16(byte[] value) => unchecked((short)ComputeCrc(value, 0, 0xA001));\n"
            + "\n"
            + "    internal static int Adler32(byte[] value)\n"
            + "    {\n"
            + "        uint a = 1;\n"
            + "        uint b = 0;\n"
            + "        foreach (var item in Require(value))\n"
            + "        {\n"
            + "            a = (a + item) % 65521;\n"
            + "            b = (b + a) % 65521;\n"
            + "        }\n"
            + "        return unchecked((int)((b << 16) | a));\n"
            + "    }\n"
            + "}"
        );
}
