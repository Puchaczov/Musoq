using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class ConvertingTests : PluginsTestBase
{
    private const string ConversionNotSupported = "CONVERSION_NOT_SUPPORTED";

    [TestMethod]
    [DynamicData(nameof(ObjectCases))]
    public void Object_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> ObjectCases()
    {
        yield return ObjectCase("ToDecimal_WithPolishCulture", library => library.ToDecimal("1,23", "pl-PL"), 1.23m);
        yield return ObjectCase("ToDecimal_NegativeWithPolishCulture", library => library.ToDecimal("-1,23", "pl-PL"), -1.23m);
        yield return ObjectCase("ToDecimal_Long", library => library.ToDecimal(64L), 64m);
        yield return ObjectCase("ToInt64_String", library => library.ToInt64("12321"), 12321L);
        yield return ObjectCase("ToInt64_NullString", library => library.ToInt64((string?)null), null);
        yield return ObjectCase("FromHex_WithPrefix", library => library.FromHex("0xFF"), 255L);
        yield return ObjectCase("FromHex_WithoutPrefix", library => library.FromHex("FF"), 255L);
        yield return ObjectCase("FromHex_Lowercase", library => library.FromHex("ff"), 255L);
        yield return ObjectCase("FromHex_SingleDigit", library => library.FromHex("A"), 10L);
        yield return ObjectCase("FromHex_Decimal16", library => library.FromHex("10"), 16L);
        yield return ObjectCase("FromHex_LowercasePrefix", library => library.FromHex("0xff"), 255L);
        yield return ObjectCase("FromHex_UppercasePrefix", library => library.FromHex("0XFF"), 255L);
        yield return ObjectCase("FromHex_MaxUnsignedLongAsSigned", library => library.FromHex("FFFFFFFFFFFFFFFF"), -1L);
        yield return ObjectCase("FromHex_Zero", library => library.FromHex("0"), 0L);
        yield return ObjectCase("FromHex_PrefixedZero", library => library.FromHex("0x0"), 0L);
        yield return ObjectCase("FromHex_Invalid", library => library.FromHex("ZZZZ"), null);
        yield return ObjectCase("FromHex_InvalidPrefixed", library => library.FromHex("0xGG"), null);
        yield return ObjectCase("FromBin_WithPrefix", library => library.FromBin("0b1010"), 10L);
        yield return ObjectCase("FromBin_WithoutPrefix", library => library.FromBin("1010"), 10L);
        yield return ObjectCase("FromBin_Short", library => library.FromBin("101"), 5L);
        yield return ObjectCase("FromBin_FourOnes", library => library.FromBin("1111"), 15L);
        yield return ObjectCase("FromBin_UppercasePrefix", library => library.FromBin("0B101"), 5L);
        yield return ObjectCase("FromBin_Zero", library => library.FromBin("0"), 0L);
        yield return ObjectCase("FromBin_PrefixedZero", library => library.FromBin("0b0"), 0L);
        yield return ObjectCase("FromBin_One", library => library.FromBin("1"), 1L);
        yield return ObjectCase("FromBin_Invalid", library => library.FromBin("102"), null);
        yield return ObjectCase("FromBin_InvalidPrefixed", library => library.FromBin("0b102"), null);
        yield return ObjectCase("FromOct_WithPrefix", library => library.FromOct("0o17"), 15L);
        yield return ObjectCase("FromOct_WithoutPrefix", library => library.FromOct("17"), 15L);
        yield return ObjectCase("FromOct_Ten", library => library.FromOct("10"), 8L);
        yield return ObjectCase("FromOct_Hundred", library => library.FromOct("100"), 64L);
        yield return ObjectCase("FromOct_SingleDigit", library => library.FromOct("7"), 7L);
        yield return ObjectCase("FromOct_TripleSeven", library => library.FromOct("777"), 511L);
        yield return ObjectCase("FromOct_UppercasePrefix", library => library.FromOct("0O10"), 8L);
        yield return ObjectCase("FromOct_Zero", library => library.FromOct("0"), 0L);
        yield return ObjectCase("FromOct_PrefixedZero", library => library.FromOct("0o0"), 0L);
        yield return ObjectCase("FromOct_Invalid", library => library.FromOct("89"), null);
        yield return ObjectCase("FromOct_InvalidDigit", library => library.FromOct("8"), null);
        yield return ObjectCase("FromOct_InvalidPrefixed", library => library.FromOct("0o8"), null);
    }

    [TestMethod]
    [DynamicData(nameof(StringCases))]
    public void String_Cases_ReturnExpected(string name, Func<LibraryBase, string?> execute, string? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> StringCases()
    {
        foreach (var testCase in ToStringCases())
            yield return testCase;
        foreach (var testCase in NumericBaseCases())
            yield return testCase;
        foreach (var testCase in HexStringCases())
            yield return testCase;
        foreach (var testCase in TextCases())
            yield return testCase;
        foreach (var testCase in Base64StringCases())
            yield return testCase;
    }

    private static IEnumerable<object?[]> ToStringCases()
    {
        yield return StringCase("ToString_DateTimeOffset", library => library.ToString(DateTimeOffset.Parse("01.01.2015 00:00:00 +00:00"), "dd.MM.yyyy HH:mm:ss zzz"), "01.01.2015 00:00:00 +00:00");
        yield return StringCase("ToString_NullDateTimeOffset", library => library.ToString((DateTimeOffset?)null), null);
        yield return StringCase("ToString_Decimal", library => library.ToString(32.22m), "32.22");
        yield return StringCase("ToString_NullDecimal", library => library.ToString((decimal?)null), null);
        yield return StringCase("ToString_Long", library => library.ToString(32L), "32");
        yield return StringCase("ToString_NullLong", library => library.ToString((long?)null), null);
        yield return StringCase("ToString_Object", library => library.ToString(new TestToStringClass()), "test class");
        yield return StringCase("ToString_NullObject", library => library.ToString((TestToStringClass?)null), null);
    }

    private static IEnumerable<object?[]> NumericBaseCases()
    {
        foreach (var testCase in ToBinaryCases())
            yield return testCase;
        foreach (var testCase in ToOctalCases())
            yield return testCase;
        foreach (var testCase in ToDecimalBaseCases())
            yield return testCase;
        foreach (var testCase in ToHexCases())
            yield return testCase;
    }

    private static IEnumerable<object?[]> ToBinaryCases()
    {
        yield return StringCase("ToBin_BooleanTrue", library => library.ToBin(true), "1");
        yield return StringCase("ToBin_BooleanFalse", library => library.ToBin(false), "0");
        yield return StringCase("ToBin_Byte", library => library.ToBin((byte)5), "101");
        yield return StringCase("ToBin_Int16", library => library.ToBin((short)10), "1010");
        yield return StringCase("ToBin_Int32", library => library.ToBin(10), "1010");
        yield return StringCase("ToBin_Int64", library => library.ToBin(10L), "1010");
        yield return StringCase("ToBin_SByte", library => library.ToBin((sbyte)5), "101");
        yield return StringCase("ToBin_UInt16", library => library.ToBin((ushort)10), "1010");
        yield return StringCase("ToBin_UInt32", library => library.ToBin((uint)10), "1010");
        yield return StringCase("ToBin_UInt64", library => library.ToBin((ulong)10), ConversionNotSupported);
        yield return StringCase("ToBin_Decimal", library => library.ToBin(123.45m), ConversionNotSupported);
        yield return StringCase("ToBin_DBNull", library => library.ToBin(DBNull.Value), ConversionNotSupported);
        yield return StringCase("ToBin_Single", library => library.ToBin(3.14f), ConversionNotSupported);
        yield return StringCase("ToBin_String", library => library.ToBin("test"), ConversionNotSupported);
        yield return StringCase("ToBin_BytesWithDelimiter", library => library.ToBin([0x01, 0x02], " "), "00000001 00000010 ");
        yield return StringCase("ToBin_NullBytes", library => library.ToBin(null), null);
    }

    private static IEnumerable<object?[]> ToOctalCases()
    {
        yield return StringCase("ToOcta_Boolean", library => library.ToOcta(true), "1");
        yield return StringCase("ToOcta_Byte", library => library.ToOcta((byte)8), "10");
        yield return StringCase("ToOcta_Int16", library => library.ToOcta((short)64), "100");
        yield return StringCase("ToOcta_Int32", library => library.ToOcta(64), "100");
        yield return StringCase("ToOcta_Int64", library => library.ToOcta(64L), "100");
        yield return StringCase("ToOcta_SByte", library => library.ToOcta((sbyte)8), "10");
        yield return StringCase("ToOcta_UInt16", library => library.ToOcta((ushort)64), "100");
        yield return StringCase("ToOcta_UInt32", library => library.ToOcta((uint)64), "100");
        yield return StringCase("ToOcta_UInt64", library => library.ToOcta((ulong)64), ConversionNotSupported);
        yield return StringCase("ToOcta_Decimal", library => library.ToOcta(123.45m), ConversionNotSupported);
        yield return StringCase("ToOcta_Single", library => library.ToOcta(3.14f), ConversionNotSupported);
        yield return StringCase("ToOcta_String", library => library.ToOcta("test"), ConversionNotSupported);
    }

    private static IEnumerable<object?[]> ToDecimalBaseCases()
    {
        yield return StringCase("ToDec_Boolean", library => library.ToDec(true), "1");
        yield return StringCase("ToDec_Byte", library => library.ToDec((byte)255), "255");
        yield return StringCase("ToDec_Char", library => library.ToDec('A'), "65");
        yield return StringCase("ToDec_Int16", library => library.ToDec((short)12345), "12345");
        yield return StringCase("ToDec_Int32", library => library.ToDec(12345), "12345");
        yield return StringCase("ToDec_Int64", library => library.ToDec(12345L), "12345");
        yield return StringCase("ToDec_SByte", library => library.ToDec((sbyte)127), "127");
        yield return StringCase("ToDec_UInt16", library => library.ToDec((ushort)65535), "65535");
        yield return StringCase("ToDec_UInt32", library => library.ToDec((uint)12345), "12345");
        yield return StringCase("ToDec_UInt64", library => library.ToDec((ulong)12345), ConversionNotSupported);
        yield return StringCase("ToDec_DateTime", library => library.ToDec(DateTime.Now), ConversionNotSupported);
        yield return StringCase("ToDec_Decimal", library => library.ToDec(123.45m), ConversionNotSupported);
        yield return StringCase("ToDec_Single", library => library.ToDec(3.14f), ConversionNotSupported);
        yield return StringCase("ToDec_String", library => library.ToDec("test"), ConversionNotSupported);
    }

    private static IEnumerable<object?[]> ToHexCases()
    {
        yield return StringCase("ToHex_BytesWithDelimiter", library => library.ToHex("Hel"u8.ToArray(), "-"), "48-65-6C");
        yield return StringCase("ToHex_EmptyBytes", library => library.ToHex([]), string.Empty);
        yield return StringCase("ToHex_NullBytes", library => library.ToHex(null), null);
        yield return StringCase("ToHex_DateTime", library => library.ToHex(DateTime.Now), ConversionNotSupported);
    }

    private static IEnumerable<object?[]> HexStringCases()
    {
        yield return StringCase("FromHexToString", library => library.FromHexToString("48656C6C6F"), "Hello");
        yield return StringCase("FromHexToString_WithEncoding", library => library.FromHexToString("48656C6C6F", "UTF-8"), "Hello");
        yield return StringCase("ToHexFromString", library => library.ToHexFromString("Hello"), "48656C6C6F");
        yield return StringCase("ToHexFromString_WithEncoding", library => library.ToHexFromString("Hello", "UTF-8"), "48656C6C6F");
    }

    private static IEnumerable<object?[]> TextCases()
    {
        yield return StringCase("ToText_NullBytes", library => library.ToText(null!, "utf-8"), string.Empty);
        yield return StringCase("ToText_EmptyBytes", library => library.ToText([], "utf-8"), string.Empty);
        yield return StringCase("ToText_Utf8", library => library.ToText("Hello"u8.ToArray(), "utf-8"), "Hello");
        yield return StringCase("ToText_Utf8Short", library => library.ToText("Hello"u8.ToArray(), "utf8"), "Hello");
        yield return StringCase("ToText_Utf16", library => library.ToText(Encoding.Unicode.GetBytes("Hello"), "utf-16"), "Hello");
        yield return StringCase("ToText_Utf16Short", library => library.ToText(Encoding.Unicode.GetBytes("Hello"), "utf16"), "Hello");
        yield return StringCase("ToText_Unicode", library => library.ToText(Encoding.Unicode.GetBytes("Hello"), "unicode"), "Hello");
        yield return StringCase("ToText_Utf16LE", library => library.ToText(Encoding.Unicode.GetBytes("Hello"), "utf-16le"), "Hello");
        yield return StringCase("ToText_Utf16LEShort", library => library.ToText(Encoding.Unicode.GetBytes("Hello"), "utf16le"), "Hello");
        yield return StringCase("ToText_Utf16BE", library => library.ToText(Encoding.BigEndianUnicode.GetBytes("Hello"), "utf-16be"), "Hello");
        yield return StringCase("ToText_Utf16BEShort", library => library.ToText(Encoding.BigEndianUnicode.GetBytes("Hello"), "utf16be"), "Hello");
        yield return StringCase("ToText_Ascii", library => library.ToText("Hello"u8.ToArray(), "ascii"), "Hello");
        yield return StringCase("ToText_Latin1", library => library.ToText(Encoding.Latin1.GetBytes("Hello"), "latin1"), "Hello");
        yield return StringCase("ToText_Iso88591", library => library.ToText(Encoding.Latin1.GetBytes("Hello"), "iso-8859-1"), "Hello");
        yield return StringCase("ToText_UnknownEncoding", library => library.ToText("Hello"u8.ToArray(), "unknown-encoding"), "Hello");
        yield return StringCase("ToText_NullEncoding", library => library.ToText("Hello"u8.ToArray(), null!), "Hello");
    }

    private static IEnumerable<object?[]> Base64StringCases()
    {
        yield return StringCase("ToBase64_Bytes", library => library.ToBase64("Hello"u8.ToArray()), "SGVsbG8=");
        yield return StringCase("ToBase64_EmptyBytes", library => library.ToBase64([]), string.Empty);
        yield return StringCase("ToBase64_BytesWithOffsetAndLength", library => library.ToBase64("Hello World"u8.ToArray(), 0, 5), "SGVsbG8=");
        yield return StringCase("ToBase64_BytesSlice", library => library.ToBase64([1, 2, 3, 4, 5], 1, 3), "AgME");
        yield return StringCase("ToBase64_String", library => library.ToBase64("Hello"), "SGVsbG8=");
        yield return StringCase("ToBase64_StringWithEncoding", library => library.ToBase64("Hello", "UTF-8"), "SGVsbG8=");
        yield return StringCase("FromBase64ToString", library => library.FromBase64ToString("SGVsbG8="), "Hello");
        yield return StringCase("FromBase64ToString_WithEncoding", library => library.FromBase64ToString("SGVsbG8=", "UTF-8"), "Hello");
    }

    [TestMethod]
    [DynamicData(nameof(NullCases))]
    public void Null_Cases_ReturnNull(string name, Func<LibraryBase, object?> execute)
    {
        Assert.IsNull(execute(Library), name);
    }

    public static IEnumerable<object?[]> NullCases()
    {
        yield return NullCase("ToBase64_NullBytes", library => library.ToBase64((byte[]?)null));
        yield return NullCase("ToBase64_NullBytesWithOffsetAndLength", library => library.ToBase64(null, 0, 5));
        yield return NullCase("ToBase64_NullString", library => library.ToBase64((string?)null));
        yield return NullCase("ToBase64_NullStringWithEncoding", library => library.ToBase64(null, "UTF-8"));
        yield return NullCase("FromBase64_Null", library => library.FromBase64(null));
        yield return NullCase("FromBase64_Empty", library => library.FromBase64(string.Empty));
        yield return NullCase("FromBase64ToString_Null", library => library.FromBase64ToString(null));
        yield return NullCase("FromBase64ToString_Empty", library => library.FromBase64ToString(string.Empty));
        yield return NullCase("FromBase64ToString_WithEncodingNull", library => library.FromBase64ToString(null, "UTF-8"));
        yield return NullCase("FromBase64ToString_WithEncodingEmpty", library => library.FromBase64ToString(string.Empty, "UTF-8"));
        yield return NullCase("FromHex_Null", library => library.FromHex(null));
        yield return NullCase("FromHex_Empty", library => library.FromHex(string.Empty));
        yield return NullCase("FromHex_Whitespace", library => library.FromHex("   "));
        yield return NullCase("FromBin_Null", library => library.FromBin(null));
        yield return NullCase("FromBin_Empty", library => library.FromBin(string.Empty));
        yield return NullCase("FromBin_Whitespace", library => library.FromBin("   "));
        yield return NullCase("FromOct_Null", library => library.FromOct(null));
        yield return NullCase("FromOct_Empty", library => library.FromOct(string.Empty));
        yield return NullCase("FromOct_Whitespace", library => library.FromOct("   "));
        yield return NullCase("FromHexToBytes_Null", library => library.FromHexToBytes(null));
        yield return NullCase("FromHexToBytes_Empty", library => library.FromHexToBytes(string.Empty));
        yield return NullCase("FromHexToBytes_OddLength", library => library.FromHexToBytes("123"));
        yield return NullCase("FromHexToBytes_Invalid", library => library.FromHexToBytes("ZZZZ"));
        yield return NullCase("FromHexToString_Null", library => library.FromHexToString(null));
        yield return NullCase("ToHexFromString_Null", library => library.ToHexFromString(null));
    }

    [TestMethod]
    [DynamicData(nameof(SupportedStringCases))]
    public void SupportedString_Cases_DoNotReturnUnsupportedMarker(string name, Func<LibraryBase, string?> execute)
    {
        var result = execute(Library);
        Assert.IsTrue(result is not null && result != ConversionNotSupported, name);
    }

    public static IEnumerable<object?[]> SupportedStringCases()
    {
        yield return SupportedCase("ToHex_BooleanTrue", library => library.ToHex(true));
        yield return SupportedCase("ToHex_BooleanFalse", library => library.ToHex(false));
        yield return SupportedCase("ToHex_Byte", library => library.ToHex((byte)255));
        yield return SupportedCase("ToHex_Char", library => library.ToHex('A'));
        yield return SupportedCase("ToHex_Decimal", library => library.ToHex(123.45m));
        yield return SupportedCase("ToHex_Double", library => library.ToHex(3.14159));
        yield return SupportedCase("ToHex_Int16", library => library.ToHex((short)12345));
        yield return SupportedCase("ToHex_Int32", library => library.ToHex(123456789));
        yield return SupportedCase("ToHex_Int64", library => library.ToHex(123456789012345L));
        yield return SupportedCase("ToHex_SByte", library => library.ToHex((sbyte)-50));
        yield return SupportedCase("ToHex_Single", library => library.ToHex(3.14f));
        yield return SupportedCase("ToHex_String", library => library.ToHex("Hello"));
        yield return SupportedCase("ToHex_UInt16", library => library.ToHex((ushort)12345));
        yield return SupportedCase("ToHex_UInt32", library => library.ToHex((uint)123456));
        yield return SupportedCase("ToHex_UInt64", library => library.ToHex((ulong)123456789));
        yield return SupportedCase("ToBin_Char", library => library.ToBin('A'));
        yield return SupportedCase("ToBin_Double", library => library.ToBin(1.0));
        yield return SupportedCase("ToOcta_Char", library => library.ToOcta('A'));
        yield return SupportedCase("ToOcta_Double", library => library.ToOcta(1.0));
    }

    [TestMethod]
    [DynamicData(nameof(ByteTextCases))]
    public void ByteText_Cases_ReturnExpectedText(string name, Func<LibraryBase, byte[]?> execute, Encoding encoding, string expected)
    {
        Assert.AreEqual(expected, encoding.GetString(execute(Library)!), name);
    }

    public static IEnumerable<object?[]> ByteTextCases()
    {
        yield return ByteTextCase("FromBase64", library => library.FromBase64("SGVsbG8="), Encoding.UTF8, "Hello");
        yield return ByteTextCase("FromHexToBytes_WithSpaces", library => library.FromHexToBytes("48 65 6C 6C 6F"), Encoding.ASCII, "Hello");
        yield return ByteTextCase("FromHexToBytes_WithDashes", library => library.FromHexToBytes("48-65-6C-6C-6F"), Encoding.ASCII, "Hello");
        yield return ByteTextCase("FromHexToBytes_WithColons", library => library.FromHexToBytes("48:65:6C:6C:6F"), Encoding.ASCII, "Hello");
        yield return ByteTextCase("FromHexToBytes_With0xPrefix", library => library.FromHexToBytes("0x48656C6C6F"), Encoding.ASCII, "Hello");
        yield return ByteTextCase("FromHexToBytes_With0XPrefix", library => library.FromHexToBytes("0X48656C6C6F"), Encoding.ASCII, "Hello");
    }

    [TestMethod]
    public void Base64RoundTrip_String_PreservesContent()
    {
        const string original = "Hello, World! \u65e5\u672c\u8a9e \U0001f30d";

        Assert.AreEqual(original, Library.FromBase64ToString(Library.ToBase64(original)));
    }

    [TestMethod]
    public void Base64RoundTrip_Bytes_PreservesContent()
    {
        var original = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE };

        CollectionAssert.AreEqual(original, Library.FromBase64(Library.ToBase64(original)));
    }

    [TestMethod]
    public void HexRoundTrip_String_PreservesContent()
    {
        const string original = "Hello, World! \u65e5\u672c\u8a9e";

        Assert.AreEqual(original, Library.FromHexToString(Library.ToHexFromString(original)));
    }

    private static object?[] ObjectCase(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        return [name, execute, expected];
    }

    private static object?[] StringCase(string name, Func<LibraryBase, string?> execute, string? expected)
    {
        return [name, execute, expected];
    }

    private static object?[] NullCase(string name, Func<LibraryBase, object?> execute)
    {
        return [name, execute];
    }

    private static object?[] SupportedCase(string name, Func<LibraryBase, string?> execute)
    {
        return [name, execute];
    }

    private static object?[] ByteTextCase(string name, Func<LibraryBase, byte[]?> execute, Encoding encoding, string expected)
    {
        return [name, execute, encoding, expected];
    }

    private sealed class TestToStringClass
    {
        public override string ToString()
        {
            return "test class";
        }
    }
}
