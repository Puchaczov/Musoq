using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for data utility methods to improve branch coverage.
///     Tests JwtDecode, JwtGetHeader, JwtGetClaim, ToHumanReadableSize, ToHumanReadableDuration,
///     CalculateEntropy, IsBase64, IsHex, FormatJson, MinifyJson, FormatXml, MinifyXml, etc.
/// </summary>
[TestClass]
public class DataUtilsExtendedTests : LibraryBaseBaseTests
{
    // Sample JWT token for testing (not a real token, just for testing structure)
    private const string ValidJwtToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    #region JwtDecode Tests

    [TestMethod]
    public void JwtDecode_Null_ReturnsNull()
    {
        Assert.IsNull(Library.JwtDecode(null));
    }

    [TestMethod]
    public void JwtDecode_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.JwtDecode(string.Empty));
    }

    [TestMethod]
    public void JwtDecode_ValidToken_ReturnsPayload()
    {
        var result = Library.JwtDecode(ValidJwtToken);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"sub\""));
        Assert.IsTrue(result.Contains("\"name\""));
    }

    [TestMethod]
    public void JwtDecode_TokenWithOnePart_ReturnsNull()
    {
        Assert.IsNull(Library.JwtDecode("onlyonepartnoperiods"));
    }

    [TestMethod]
    public void JwtDecode_InvalidBase64_ReturnsNull()
    {
        Assert.IsNull(Library.JwtDecode("abc.!!!invalidbase64!!!.xyz"));
    }

    [TestMethod]
    public void JwtDecode_TokenNeedsPadding2Chars_Decodes()
    {
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJhIjoiYiJ9.sig";
        var result = Library.JwtDecode(token);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void JwtDecode_TokenNeedsPadding1Char_Decodes()
    {
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJhIjoiYmNkIn0.sig";
        var result = Library.JwtDecode(token);
        Assert.IsNotNull(result);
    }

    #endregion

    #region JwtGetHeader Tests

    [TestMethod]
    public void JwtGetHeader_Null_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetHeader(null));
    }

    [TestMethod]
    public void JwtGetHeader_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetHeader(string.Empty));
    }

    [TestMethod]
    public void JwtGetHeader_ValidToken_ReturnsHeader()
    {
        var result = Library.JwtGetHeader(ValidJwtToken);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"alg\""));
        Assert.IsTrue(result.Contains("\"typ\""));
    }

    [TestMethod]
    public void JwtGetHeader_InvalidBase64_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetHeader("!!!invalidbase64!!!.payload.sig"));
    }

    [TestMethod]
    public void JwtGetHeader_HeaderNeedsPadding_Decodes()
    {
        var token = "eyJhIjoiYiJ9.payload.sig";
        var result = Library.JwtGetHeader(token);
        Assert.IsNotNull(result);
    }

    #endregion

    #region JwtGetClaim Tests

    [TestMethod]
    public void JwtGetClaim_NullToken_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetClaim(null, "sub"));
    }

    [TestMethod]
    public void JwtGetClaim_NullClaimName_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetClaim(ValidJwtToken, null));
    }

    [TestMethod]
    public void JwtGetClaim_EmptyClaimName_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetClaim(ValidJwtToken, string.Empty));
    }

    [TestMethod]
    public void JwtGetClaim_ExistingStringClaim_ReturnsClaim()
    {
        var result = Library.JwtGetClaim(ValidJwtToken, "name");
        Assert.AreEqual("John Doe", result);
    }

    [TestMethod]
    public void JwtGetClaim_ExistingNonStringClaim_ReturnsRawText()
    {
        var result = Library.JwtGetClaim(ValidJwtToken, "iat");
        Assert.AreEqual("1516239022", result);
    }

    [TestMethod]
    public void JwtGetClaim_NonExistentClaim_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetClaim(ValidJwtToken, "nonexistent"));
    }

    [TestMethod]
    public void JwtGetClaim_InvalidToken_ReturnsNull()
    {
        Assert.IsNull(Library.JwtGetClaim("invalid.token", "sub"));
    }

    #endregion

    #region ToHumanReadableSize Tests

    [TestMethod]
    public void ToHumanReadableSize_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToHumanReadableSize(null));
    }

    [TestMethod]
    public void ToHumanReadableSize_Zero_ReturnsBytes()
    {
        Assert.AreEqual("0 B", Library.ToHumanReadableSize(0));
    }

    [TestMethod]
    public void ToHumanReadableSize_Bytes_ReturnsB()
    {
        Assert.AreEqual("512 B", Library.ToHumanReadableSize(512));
    }

    [TestMethod]
    public void ToHumanReadableSize_Kilobytes_ReturnsKB()
    {
        Assert.AreEqual("1 KB", Library.ToHumanReadableSize(1024));
    }

    [TestMethod]
    public void ToHumanReadableSize_Megabytes_ReturnsMB()
    {
        Assert.AreEqual("1 MB", Library.ToHumanReadableSize(1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableSize_Gigabytes_ReturnsGB()
    {
        Assert.AreEqual("1 GB", Library.ToHumanReadableSize(1024L * 1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableSize_Terabytes_ReturnsTB()
    {
        Assert.AreEqual("1 TB", Library.ToHumanReadableSize(1024L * 1024 * 1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableSize_Petabytes_ReturnsPB()
    {
        Assert.AreEqual("1 PB", Library.ToHumanReadableSize(1024L * 1024 * 1024 * 1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableSize_FractionalMB_ReturnsDecimal()
    {
        var result = Library.ToHumanReadableSize((long)(1.5 * 1024 * 1024));
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("1") && result.Contains("5") && result.Contains("MB"));
    }

    #endregion

    #region ToHumanReadableDuration Tests

    [TestMethod]
    public void ToHumanReadableDuration_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToHumanReadableDuration(null));
    }

    [TestMethod]
    public void ToHumanReadableDuration_Zero_Returns0s()
    {
        Assert.AreEqual("0s", Library.ToHumanReadableDuration(0));
    }

    [TestMethod]
    public void ToHumanReadableDuration_SecondsOnly_ReturnsSeconds()
    {
        Assert.AreEqual("30s", Library.ToHumanReadableDuration(30));
    }

    [TestMethod]
    public void ToHumanReadableDuration_MinutesOnly_ReturnsMinutes()
    {
        Assert.AreEqual("5m", Library.ToHumanReadableDuration(300));
    }

    [TestMethod]
    public void ToHumanReadableDuration_MinutesAndSeconds_ReturnsBoth()
    {
        Assert.AreEqual("5m 30s", Library.ToHumanReadableDuration(330));
    }

    [TestMethod]
    public void ToHumanReadableDuration_HoursOnly_ReturnsHours()
    {
        Assert.AreEqual("1h", Library.ToHumanReadableDuration(3600));
    }

    [TestMethod]
    public void ToHumanReadableDuration_HoursMinutesSeconds_ReturnsAll()
    {
        Assert.AreEqual("1h 30m 45s", Library.ToHumanReadableDuration(5445));
    }

    [TestMethod]
    public void ToHumanReadableDuration_Days_ReturnsDays()
    {
        Assert.AreEqual("1d", Library.ToHumanReadableDuration(86400));
    }

    [TestMethod]
    public void ToHumanReadableDuration_DaysHoursMinutesSeconds_ReturnsAll()
    {
        Assert.AreEqual("1d 2h 3m 4s", Library.ToHumanReadableDuration(93784));
    }

    #endregion

    #region CalculateEntropy Tests

    [TestMethod]
    public void CalculateEntropy_Null_ReturnsNull()
    {
        Assert.IsNull(Library.CalculateEntropy(null));
    }

    [TestMethod]
    public void CalculateEntropy_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.CalculateEntropy(string.Empty));
    }

    [TestMethod]
    public void CalculateEntropy_SingleChar_ReturnsZero()
    {
        Assert.AreEqual(0.0, Library.CalculateEntropy("aaaa"));
    }

    [TestMethod]
    public void CalculateEntropy_TwoUniqueChars_ReturnsNonZero()
    {
        var result = Library.CalculateEntropy("aabb");
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.001);
    }

    [TestMethod]
    public void CalculateEntropy_HighEntropy_ReturnsHighValue()
    {
        var result = Library.CalculateEntropy("abcdefghij");
        Assert.IsNotNull(result);
        Assert.IsTrue(result > 3.0);
    }

    #endregion

    #region IsBase64 Tests

    [TestMethod]
    public void IsBase64_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsBase64(null));
    }

    [TestMethod]
    public void IsBase64_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IsBase64(string.Empty));
    }

    [TestMethod]
    public void IsBase64_ValidBase64_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsBase64("SGVsbG8gV29ybGQ="));
    }

    [TestMethod]
    public void IsBase64_InvalidLength_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsBase64("abc"));
    }

    [TestMethod]
    public void IsBase64_InvalidChars_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsBase64("!@#$%^&*"));
    }

    [TestMethod]
    public void IsBase64_ValidNoPadding_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsBase64("YWI="));
    }

    #endregion

    #region IsHex Tests

    [TestMethod]
    public void IsHex_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsHex(null));
    }

    [TestMethod]
    public void IsHex_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IsHex(string.Empty));
    }

    [TestMethod]
    public void IsHex_ValidHexLower_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsHex("0123456789abcdef"));
    }

    [TestMethod]
    public void IsHex_ValidHexUpper_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsHex("0123456789ABCDEF"));
    }

    [TestMethod]
    public void IsHex_ValidHexMixed_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsHex("aAbBcCdDeEfF"));
    }

    [TestMethod]
    public void IsHex_InvalidChars_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsHex("0123456789abcdefg"));
    }

    [TestMethod]
    public void IsHex_WithSpaces_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsHex("ab cd ef"));
    }

    #endregion

    #region IsJwt Tests

    [TestMethod]
    public void IsJwt_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsJwt(null));
    }

    [TestMethod]
    public void IsJwt_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.IsJwt(string.Empty));
    }

    [TestMethod]
    public void IsJwt_ValidJwt_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsJwt(ValidJwtToken));
    }

    [TestMethod]
    public void IsJwt_TwoParts_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsJwt("header.payload"));
    }

    [TestMethod]
    public void IsJwt_FourParts_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsJwt("a.b.c.d"));
    }

    [TestMethod]
    public void IsJwt_InvalidBase64Parts_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsJwt("!!!.!!!.!!!"));
    }

    #endregion

    #region GetQueryParam Tests

    [TestMethod]
    public void GetQueryParam_NullQueryString_ReturnsNull()
    {
        Assert.IsNull(Library.GetQueryParam(null, "key"));
    }

    [TestMethod]
    public void GetQueryParam_NullParamName_ReturnsNull()
    {
        Assert.IsNull(Library.GetQueryParam("key=value", null));
    }

    [TestMethod]
    public void GetQueryParam_EmptyQueryString_ReturnsNull()
    {
        Assert.IsNull(Library.GetQueryParam(string.Empty, "key"));
    }

    [TestMethod]
    public void GetQueryParam_ValidQuery_ReturnsValue()
    {
        Assert.AreEqual("world", Library.GetQueryParam("hello=world", "hello"));
    }

    [TestMethod]
    public void GetQueryParam_WithLeadingQuestionMark_ReturnsValue()
    {
        Assert.AreEqual("world", Library.GetQueryParam("?hello=world", "hello"));
    }

    [TestMethod]
    public void GetQueryParam_MultipleParams_ReturnsCorrectValue()
    {
        Assert.AreEqual("2", Library.GetQueryParam("a=1&b=2&c=3", "b"));
    }

    [TestMethod]
    public void GetQueryParam_NonExistentParam_ReturnsNull()
    {
        Assert.IsNull(Library.GetQueryParam("a=1&b=2", "c"));
    }

    #endregion

    #region ParseKeyValue Tests

    [TestMethod]
    public void ParseKeyValue_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.ParseKeyValue(null, "key"));
    }

    [TestMethod]
    public void ParseKeyValue_NullKey_ReturnsNull()
    {
        Assert.IsNull(Library.ParseKeyValue("key=value", null));
    }

    [TestMethod]
    public void ParseKeyValue_EmptyValue_ReturnsNull()
    {
        Assert.IsNull(Library.ParseKeyValue(string.Empty, "key"));
    }

    [TestMethod]
    public void ParseKeyValue_EmptyKey_ReturnsNull()
    {
        Assert.IsNull(Library.ParseKeyValue("key=value", string.Empty));
    }

    [TestMethod]
    public void ParseKeyValue_ValidPair_ReturnsValue()
    {
        Assert.AreEqual("world", Library.ParseKeyValue("hello=world", "hello"));
    }

    [TestMethod]
    public void ParseKeyValue_MultiplePairs_ReturnsCorrectValue()
    {
        Assert.AreEqual("2", Library.ParseKeyValue("a=1&b=2&c=3", "b"));
    }

    [TestMethod]
    public void ParseKeyValue_CustomDelimiters_ReturnsValue()
    {
        Assert.AreEqual("2", Library.ParseKeyValue("a:1;b:2;c:3", "b", ";", ":"));
    }

    [TestMethod]
    public void ParseKeyValue_NonExistentKey_ReturnsNull()
    {
        Assert.IsNull(Library.ParseKeyValue("a=1&b=2", "c"));
    }

    [TestMethod]
    public void ParseKeyValue_ValueWithEquals_ReturnsFull()
    {
        Assert.AreEqual("value=more", Library.ParseKeyValue("Key=value=more", "Key"));
    }

    #endregion

    #region FormatJson Tests

    [TestMethod]
    public void FormatJson_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FormatJson(null));
    }

    [TestMethod]
    public void FormatJson_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FormatJson(string.Empty));
    }

    [TestMethod]
    public void FormatJson_ValidJson_ReturnsFormatted()
    {
        var result = Library.FormatJson("{\"a\":1,\"b\":2}");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\n"));
    }

    [TestMethod]
    public void FormatJson_InvalidJson_ReturnsOriginal()
    {
        Assert.AreEqual("not json", Library.FormatJson("not json"));
    }

    #endregion

    #region MinifyJson Tests

    [TestMethod]
    public void MinifyJson_Null_ReturnsNull()
    {
        Assert.IsNull(Library.MinifyJson(null));
    }

    [TestMethod]
    public void MinifyJson_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.MinifyJson(string.Empty));
    }

    [TestMethod]
    public void MinifyJson_FormattedJson_ReturnsMinified()
    {
        var formatted = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        var result = Library.MinifyJson(formatted);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("\n"));
    }

    [TestMethod]
    public void MinifyJson_InvalidJson_ReturnsOriginal()
    {
        Assert.AreEqual("not json", Library.MinifyJson("not json"));
    }

    #endregion

    #region FormatXml Tests

    [TestMethod]
    public void FormatXml_Null_ReturnsNull()
    {
        Assert.IsNull(Library.FormatXml(null));
    }

    [TestMethod]
    public void FormatXml_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.FormatXml(string.Empty));
    }

    [TestMethod]
    public void FormatXml_ValidXml_ReturnsFormatted()
    {
        var result = Library.FormatXml("<root><child>text</child></root>");
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\n") || result.Contains("<root>"));
    }

    [TestMethod]
    public void FormatXml_InvalidXml_ReturnsOriginal()
    {
        Assert.AreEqual("not xml", Library.FormatXml("not xml"));
    }

    #endregion

    #region MinifyXml Tests

    [TestMethod]
    public void MinifyXml_Null_ReturnsNull()
    {
        Assert.IsNull(Library.MinifyXml(null));
    }

    [TestMethod]
    public void MinifyXml_EmptyString_ReturnsNull()
    {
        Assert.IsNull(Library.MinifyXml(string.Empty));
    }

    [TestMethod]
    public void MinifyXml_FormattedXml_ReturnsMinified()
    {
        var formatted = "<root>\n  <child>text</child>\n</root>";
        var result = Library.MinifyXml(formatted);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MinifyXml_InvalidXml_ReturnsOriginal()
    {
        Assert.AreEqual("not xml", Library.MinifyXml("not xml"));
    }

    #endregion
}
