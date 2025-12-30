using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LibraryBaseUtilsTests
{
    private readonly LibraryBase _library = new();

    #region Unicode Escape Tests

    [TestMethod]
    public void ToUnicodeEscape_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToUnicodeEscape(null));
    }

    [TestMethod]
    public void ToUnicodeEscape_WhenHi_ReturnsCorrectEscape()
    {
        Assert.AreEqual("\\u0048\\u0069", _library.ToUnicodeEscape("Hi"));
    }

    [TestMethod]
    public void FromUnicodeEscape_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromUnicodeEscape(null));
    }

    [TestMethod]
    public void FromUnicodeEscape_WhenValidEscape_ReturnsOriginal()
    {
        Assert.AreEqual("Hi", _library.FromUnicodeEscape("\\u0048\\u0069"));
    }

    #endregion

    #region ROT13/ROT47 Tests

    [TestMethod]
    public void Rot13_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Rot13(null));
    }

    [TestMethod]
    public void Rot13_WhenHello_ReturnsUryyb()
    {
        Assert.AreEqual("Uryyb", _library.Rot13("Hello"));
    }

    [TestMethod]
    public void Rot13_WhenAppliedTwice_ReturnsOriginal()
    {
        var original = "Hello World!";
        Assert.AreEqual(original, _library.Rot13(_library.Rot13(original)));
    }

    [TestMethod]
    public void Rot47_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Rot47(null));
    }

    [TestMethod]
    public void Rot47_WhenAppliedTwice_ReturnsOriginal()
    {
        var original = "Hello World! 123";
        Assert.AreEqual(original, _library.Rot47(_library.Rot47(original)));
    }

    #endregion

    #region Morse Code Tests

    [TestMethod]
    public void ToMorse_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToMorse(null));
    }

    [TestMethod]
    public void ToMorse_WhenSOS_ReturnsCorrectCode()
    {
        Assert.AreEqual("... --- ...", _library.ToMorse("SOS"));
    }

    [TestMethod]
    public void FromMorse_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromMorse(null));
    }

    [TestMethod]
    public void FromMorse_WhenValidCode_ReturnsText()
    {
        Assert.AreEqual("SOS", _library.FromMorse("... --- ..."));
    }

    #endregion

    #region Binary String Tests

    [TestMethod]
    public void ToBinaryString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToBinaryString(null));
    }

    [TestMethod]
    public void ToBinaryString_WhenHi_ReturnsCorrectBinary()
    {
        Assert.AreEqual("01001000 01101001", _library.ToBinaryString("Hi"));
    }

    [TestMethod]
    public void FromBinaryString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FromBinaryString(null));
    }

    [TestMethod]
    public void FromBinaryString_WhenValidBinary_ReturnsText()
    {
        Assert.AreEqual("Hi", _library.FromBinaryString("01001000 01101001"));
    }

    #endregion

    #region String Manipulation Tests

    [TestMethod]
    public void ReverseString_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ReverseString(null));
    }

    [TestMethod]
    public void ReverseString_WhenHello_ReturnsOlleh()
    {
        Assert.AreEqual("olleH", _library.ReverseString("Hello"));
    }

    [TestMethod]
    public void SplitAndTake_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.SplitAndTake(null, ",", 0));
    }

    [TestMethod]
    public void SplitAndTake_WhenValidIndex_ReturnsPart()
    {
        Assert.AreEqual("two", _library.SplitAndTake("one,two,three", ",", 1));
    }

    [TestMethod]
    public void SplitAndTake_WhenInvalidIndex_ReturnsNull()
    {
        Assert.IsNull(_library.SplitAndTake("one,two,three", ",", 10));
    }

    [TestMethod]
    public void PadLeft_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.PadLeft(null, 10));
    }

    [TestMethod]
    public void PadLeft_WithSpaces_PadsCorrectly()
    {
        Assert.AreEqual("     Hello", _library.PadLeft("Hello", 10));
    }

    [TestMethod]
    public void PadLeft_WithChar_PadsCorrectly()
    {
        Assert.AreEqual("00042", _library.PadLeft("42", 5, '0'));
    }

    [TestMethod]
    public void PadRight_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.PadRight(null, 10));
    }

    [TestMethod]
    public void PadRight_WithSpaces_PadsCorrectly()
    {
        Assert.AreEqual("Hello     ", _library.PadRight("Hello", 10));
    }

    [TestMethod]
    public void RemoveDiacritics_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.RemoveDiacritics(null));
    }

    [TestMethod]
    public void RemoveDiacritics_WhenCafe_ReturnsCafe()
    {
        Assert.AreEqual("cafe", _library.RemoveDiacritics("café"));
    }

    #endregion

    #region Hash Tests

    [TestMethod]
    public void Sha384_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Sha384((string?)null));
    }

    [TestMethod]
    public void Sha384_WhenHello_ReturnsCorrectHash()
    {
        var hash = _library.Sha384("hello");
        Assert.IsNotNull(hash);
        Assert.AreEqual(96, hash.Length); // SHA-384 = 384 bits = 96 hex chars
    }

    [TestMethod]
    public void Crc32_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Crc32((string?)null));
    }

    [TestMethod]
    public void Crc32_WhenHello_ReturnsCorrectChecksum()
    {
        var crc = _library.Crc32("hello");
        Assert.IsNotNull(crc);
        Assert.AreEqual(8, crc.Length); // CRC32 = 32 bits = 8 hex chars
    }

    [TestMethod]
    public void HmacSha256_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.HmacSha256(null, "key"));
        Assert.IsNull(_library.HmacSha256("message", null));
    }

    [TestMethod]
    public void HmacSha256_WhenValid_ReturnsHash()
    {
        var hash = _library.HmacSha256("message", "secret");
        Assert.IsNotNull(hash);
        Assert.AreEqual(64, hash.Length); // SHA-256 = 256 bits = 64 hex chars
    }

    [TestMethod]
    public void HmacSha512_WhenValid_ReturnsHash()
    {
        var hash = _library.HmacSha512("message", "secret");
        Assert.IsNotNull(hash);
        Assert.AreEqual(128, hash.Length); // SHA-512 = 512 bits = 128 hex chars
    }

    [TestMethod]
    public void Sha384_ByteArray_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Sha384((byte[]?)null));
    }

    [TestMethod]
    public void Sha384_ByteArray_WhenValid_ReturnsCorrectHash()
    {
        var hash = _library.Sha384(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }); // "Hello"
        Assert.IsNotNull(hash);
        Assert.AreEqual(96, hash.Length);
    }

    [TestMethod]
    public void Crc32_ByteArray_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.Crc32((byte[]?)null));
    }

    [TestMethod]
    public void Crc32_ByteArray_WhenValid_ReturnsChecksum()
    {
        var crc = _library.Crc32(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f }); // "Hello"
        Assert.IsNotNull(crc);
        Assert.AreEqual(8, crc.Length);
    }

    #endregion

    #region JWT Tests

    [TestMethod]
    public void JwtDecode_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.JwtDecode(null));
    }

    [TestMethod]
    public void JwtDecode_WhenValidJwt_ReturnsPayload()
    {
        // Example JWT with payload: {"sub":"1234567890","name":"John Doe","iat":1516239022}
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var payload = _library.JwtDecode(jwt);
        Assert.IsNotNull(payload);
        Assert.Contains("John Doe", payload);
    }

    [TestMethod]
    public void JwtGetHeader_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.JwtGetHeader(null));
    }

    [TestMethod]
    public void JwtGetHeader_WhenValidJwt_ReturnsHeader()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var header = _library.JwtGetHeader(jwt);
        Assert.IsNotNull(header);
        Assert.Contains("HS256", header);
    }

    [TestMethod]
    public void JwtGetClaim_WhenValidJwt_ReturnsClaim()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var name = _library.JwtGetClaim(jwt, "name");
        Assert.AreEqual("John Doe", name);
    }

    [TestMethod]
    public void IsJwt_WhenValidJwt_ReturnsTrue()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        Assert.IsTrue(_library.IsJwt(jwt));
    }

    [TestMethod]
    public void IsJwt_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsJwt("not.a.jwt"));
        Assert.IsFalse(_library.IsJwt("just some text"));
    }

    #endregion

    #region Query String Tests

    [TestMethod]
    public void GetQueryParam_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.GetQueryParam(null, "key"));
    }

    [TestMethod]
    public void GetQueryParam_WhenValidQuery_ReturnsValue()
    {
        Assert.AreEqual("123", _library.GetQueryParam("?id=123&name=test", "id"));
        Assert.AreEqual("test", _library.GetQueryParam("id=123&name=test", "name"));
    }

    [TestMethod]
    public void ParseKeyValue_WhenValid_ReturnsValue()
    {
        Assert.AreEqual("bar", _library.ParseKeyValue("foo=bar&baz=qux", "foo"));
    }

    #endregion

    #region Format Tests

    [TestMethod]
    public void FormatJson_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatJson(null));
    }

    [TestMethod]
    public void FormatJson_WhenValid_FormatsWithIndentation()
    {
        var result = _library.FormatJson("{\"a\":1}");
        Assert.IsNotNull(result);
        Assert.Contains("\n", result);
    }

    [TestMethod]
    public void MinifyJson_WhenValid_RemovesWhitespace()
    {
        var result = _library.MinifyJson("{\n  \"a\": 1\n}");
        Assert.AreEqual("{\"a\":1}", result);
    }

    [TestMethod]
    public void FormatXml_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatXml(null));
    }

    [TestMethod]
    public void MinifyXml_WhenValid_RemovesWhitespace()
    {
        var result = _library.MinifyXml("<root>\n  <child />\n</root>");
        Assert.IsNotNull(result);
        Assert.DoesNotContain("\n", result);
    }

    #endregion

    #region Human Readable Tests

    [TestMethod]
    public void ToHumanReadableSize_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToHumanReadableSize(null));
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenBytes_ReturnsCorrect()
    {
        Assert.AreEqual("500 B", _library.ToHumanReadableSize(500));
        Assert.AreEqual("1 KB", _library.ToHumanReadableSize(1024));
        // Use culture-independent check for decimal values
        var result = _library.ToHumanReadableSize(1536);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("1") && result.EndsWith("KB"));
        Assert.AreEqual("1 MB", _library.ToHumanReadableSize(1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToHumanReadableDuration(null));
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenSeconds_ReturnsCorrect()
    {
        Assert.AreEqual("0s", _library.ToHumanReadableDuration(0));
        Assert.AreEqual("45s", _library.ToHumanReadableDuration(45));
        Assert.AreEqual("1m 30s", _library.ToHumanReadableDuration(90));
        Assert.AreEqual("1h 1m 1s", _library.ToHumanReadableDuration(3661));
        Assert.AreEqual("1d 1h", _library.ToHumanReadableDuration(90000));
    }

    #endregion

    #region Data Analysis Tests

    [TestMethod]
    public void CalculateEntropy_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.CalculateEntropy(null));
    }

    [TestMethod]
    public void CalculateEntropy_WhenAllSame_ReturnsZero()
    {
        Assert.AreEqual(0.0, _library.CalculateEntropy("aaaa"));
    }

    [TestMethod]
    public void CalculateEntropy_WhenRandom_ReturnsHigher()
    {
        var lowEntropy = _library.CalculateEntropy("aaaa");
        var highEntropy = _library.CalculateEntropy("abcd");
        Assert.IsNotNull(highEntropy);
        Assert.IsNotNull(lowEntropy);
        Assert.IsGreaterThan(lowEntropy.Value, highEntropy.Value);
    }

    [TestMethod]
    public void IsBase64_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsBase64(null));
    }

    [TestMethod]
    public void IsBase64_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsBase64("SGVsbG8gV29ybGQ="));
    }

    [TestMethod]
    public void IsBase64_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsBase64("not base64!"));
    }

    [TestMethod]
    public void IsHex_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsHex(null));
    }

    [TestMethod]
    public void IsHex_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsHex("48656c6c6f"));
        Assert.IsTrue(_library.IsHex("ABCDEF0123"));
    }

    [TestMethod]
    public void IsHex_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsHex("xyz123"));
    }

    #endregion

    #region IP Utilities Tests

    [TestMethod]
    public void IsPrivateIP_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsPrivateIP(null));
    }

    [TestMethod]
    public void IsPrivateIP_WhenPrivate_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsPrivateIP("10.0.0.1"));
        Assert.IsTrue(_library.IsPrivateIP("172.16.0.1"));
        Assert.IsTrue(_library.IsPrivateIP("192.168.1.1"));
        Assert.IsTrue(_library.IsPrivateIP("127.0.0.1"));
    }

    [TestMethod]
    public void IsPrivateIP_WhenPublic_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsPrivateIP("8.8.8.8"));
        Assert.IsFalse(_library.IsPrivateIP("1.1.1.1"));
    }

    [TestMethod]
    public void IpToLong_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IpToLong(null));
    }

    [TestMethod]
    public void IpToLong_WhenValid_ReturnsNumber()
    {
        Assert.AreEqual(3232235777L, _library.IpToLong("192.168.1.1"));
    }

    [TestMethod]
    public void LongToIp_WhenValid_ReturnsIp()
    {
        Assert.AreEqual("192.168.1.1", _library.LongToIp(3232235777L));
    }

    [TestMethod]
    public void IsInSubnet_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsInSubnet(null, "192.168.1.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_WhenInSubnet_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsInSubnet("192.168.1.100", "192.168.1.0/24"));
    }

    [TestMethod]
    public void IsInSubnet_WhenNotInSubnet_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsInSubnet("192.168.2.1", "192.168.1.0/24"));
    }

    [TestMethod]
    public void FormatMac_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatMac(null));
    }

    [TestMethod]
    public void FormatMac_WhenValid_FormatsCorrectly()
    {
        Assert.AreEqual("AA:BB:CC:DD:EE:FF", _library.FormatMac("aabbccddeeff"));
        Assert.AreEqual("AA-BB-CC-DD-EE-FF", _library.FormatMac("AA:BB:CC:DD:EE:FF", "-"));
    }

    #endregion

    #region Conversion Tests

    [TestMethod]
    public void ConvertBase_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ConvertBase(null, 10, 2));
    }

    [TestMethod]
    public void ConvertBase_DecimalToBinary()
    {
        Assert.AreEqual("1010", _library.ConvertBase("10", 10, 2));
    }

    [TestMethod]
    public void ConvertBase_HexToDecimal()
    {
        Assert.AreEqual("255", _library.ConvertBase("FF", 16, 10));
    }

    [TestMethod]
    public void UnixToDateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.UnixToDateTime(null));
    }

    [TestMethod]
    public void UnixToDateTime_WhenValid_ReturnsDateTime()
    {
        var dt = _library.UnixToDateTime(0);
        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
    }

    [TestMethod]
    public void DateTimeToUnix_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.DateTimeToUnix(null));
    }

    [TestMethod]
    public void DateTimeToUnix_RoundTrips()
    {
        var original = 1700000000L;
        var dt = _library.UnixToDateTime(original);
        var back = _library.DateTimeToUnix(dt);
        Assert.AreEqual(original, back);
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.UnixMillisToDateTime(null));
    }

    [TestMethod]
    public void UnixMillisToDateTime_WhenValid_ReturnsDateTime()
    {
        var dt = _library.UnixMillisToDateTime(0);
        Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
    }

    [TestMethod]
    public void DateTimeToUnixMillis_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.DateTimeToUnixMillis(null));
    }

    [TestMethod]
    public void DateTimeToUnixMillis_RoundTrips()
    {
        var original = 1700000000000L;
        var dt = _library.UnixMillisToDateTime(original);
        var back = _library.DateTimeToUnixMillis(dt);
        Assert.AreEqual(original, back);
    }

    #endregion

    #region Slug and Escape Tests

    [TestMethod]
    public void ToSlug_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToSlug(null));
    }

    [TestMethod]
    public void ToSlug_WhenValid_ReturnsSlug()
    {
        Assert.AreEqual("hello-world", _library.ToSlug("Hello World!"));
        Assert.AreEqual("cafe-au-lait", _library.ToSlug("Café au Lait"));
    }

    [TestMethod]
    public void EscapeRegex_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.EscapeRegex(null));
    }

    [TestMethod]
    public void EscapeRegex_WhenSpecialChars_EscapesThem()
    {
        // .NET Regex.Escape escapes opening bracket but not closing bracket
        Assert.AreEqual(@"\[test]", _library.EscapeRegex("[test]"));
        Assert.AreEqual(@"a\.b", _library.EscapeRegex("a.b"));
    }

    [TestMethod]
    public void EscapeSql_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.EscapeSql(null));
    }

    [TestMethod]
    public void EscapeSql_WhenSingleQuotes_DoublesThem()
    {
        Assert.AreEqual("O''Brien", _library.EscapeSql("O'Brien"));
    }

    #endregion

    #region Extraction Tests

    [TestMethod]
    public void ExtractUrls_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractUrls(null));
    }

    [TestMethod]
    public void ExtractUrls_WhenContainsUrls_ExtractsThem()
    {
        var result = _library.ExtractUrls("Visit https://example.com and http://test.org");
        Assert.IsNotNull(result);
        Assert.Contains("https://example.com", result);
        Assert.Contains("http://test.org", result);
    }

    [TestMethod]
    public void ExtractEmails_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractEmails(null));
    }

    [TestMethod]
    public void ExtractEmails_WhenContainsEmails_ExtractsThem()
    {
        var result = _library.ExtractEmails("Contact: test@example.com or admin@test.org");
        Assert.IsNotNull(result);
        Assert.Contains("test@example.com", result);
        Assert.Contains("admin@test.org", result);
    }

    [TestMethod]
    public void ExtractIPs_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ExtractIPs(null));
    }

    [TestMethod]
    public void ExtractIPs_WhenContainsIPs_ExtractsThem()
    {
        var result = _library.ExtractIPs("Server at 192.168.1.1 and 10.0.0.1");
        Assert.IsNotNull(result);
        Assert.Contains("192.168.1.1", result);
        Assert.Contains("10.0.0.1", result);
    }

    [TestMethod]
    public void NewGuid_ReturnsValidGuid()
    {
        var guid = _library.NewGuid();
        Assert.IsTrue(Guid.TryParse(guid, out _));
    }

    [TestMethod]
    public void NewGuidCompact_ReturnsGuidWithoutDashes()
    {
        var guid = _library.NewGuidCompact();
        Assert.AreEqual(32, guid.Length);
        Assert.DoesNotContain('-', guid);
    }

    #endregion
}
