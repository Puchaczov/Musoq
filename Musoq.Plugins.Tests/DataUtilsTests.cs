using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class DataUtilsTests : LibraryBaseBaseTests
{
    #region JwtDecode Tests

    [TestMethod]
    public void JwtDecode_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenInvalidFormatProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("not-a-jwt");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenOnlyOnePartProvided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("eyJhbGciOiJIUzI1NiJ9");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtDecode_WhenValidJwtProvided_ShouldReturnPayload()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtDecode(jwt);

        Assert.IsNotNull(result);
        Assert.Contains("1234567890", result);
        Assert.Contains("John Doe", result);
    }

    [TestMethod]
    public void JwtDecode_WhenInvalidBase64Provided_ShouldReturnNull()
    {
        var result = Library.JwtDecode("invalid.!!!invalid!!!.token");

        Assert.IsNull(result);
    }

    #endregion

    #region JwtGetHeader Tests

    [TestMethod]
    public void JwtGetHeader_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenValidJwtProvided_ShouldReturnHeader()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetHeader(jwt);

        Assert.IsNotNull(result);
        Assert.Contains("HS256", result);
        Assert.Contains("JWT", result);
    }

    [TestMethod]
    public void JwtGetHeader_WhenNoPartsProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetHeader("");

        Assert.IsNull(result);
    }

    #endregion

    #region JwtGetClaim Tests

    [TestMethod]
    public void JwtGetClaim_WhenNullTokenProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetClaim(null, "sub");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNullClaimNameProvided_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenEmptyTokenProvided_ShouldReturnNull()
    {
        var result = Library.JwtGetClaim(string.Empty, "sub");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenEmptyClaimNameProvided_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenValidClaimRequested_ShouldReturnValue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "sub");

        Assert.AreEqual("1234567890", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenStringClaimRequested_ShouldReturnString()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "name");

        Assert.AreEqual("John Doe", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNumericClaimRequested_ShouldReturnRawValue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.JwtGetClaim(jwt, "iat");

        Assert.AreEqual("1516239022", result);
    }

    [TestMethod]
    public void JwtGetClaim_WhenNonExistentClaimRequested_ShouldReturnNull()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var result = Library.JwtGetClaim(jwt, "nonexistent");

        Assert.IsNull(result);
    }

    #endregion

    #region GetQueryParam Tests

    [TestMethod]
    public void GetQueryParam_WhenNullQueryProvided_ShouldReturnNull()
    {
        var result = Library.GetQueryParam(null, "param");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetQueryParam_WhenNullParamNameProvided_ShouldReturnNull()
    {
        var result = Library.GetQueryParam("key=value", null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetQueryParam_WhenEmptyQueryProvided_ShouldReturnNull()
    {
        var result = Library.GetQueryParam(string.Empty, "param");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetQueryParam_WhenEmptyParamNameProvided_ShouldReturnNull()
    {
        var result = Library.GetQueryParam("key=value", string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetQueryParam_WhenValidQueryWithQuestionMark_ShouldReturnValue()
    {
        var result = Library.GetQueryParam("?name=John&age=30", "name");

        Assert.AreEqual("John", result);
    }

    [TestMethod]
    public void GetQueryParam_WhenValidQueryWithoutQuestionMark_ShouldReturnValue()
    {
        var result = Library.GetQueryParam("name=John&age=30", "age");

        Assert.AreEqual("30", result);
    }

    [TestMethod]
    public void GetQueryParam_WhenParamNotFound_ShouldReturnNull()
    {
        var result = Library.GetQueryParam("name=John&age=30", "email");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetQueryParam_WhenEncodedValueProvided_ShouldDecode()
    {
        var result = Library.GetQueryParam("name=John%20Doe", "name");

        Assert.AreEqual("John Doe", result);
    }

    #endregion

    #region ParseKeyValue Tests

    [TestMethod]
    public void ParseKeyValue_WhenNullValueProvided_ShouldReturnNull()
    {
        var result = Library.ParseKeyValue(null, "key");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenNullKeyProvided_ShouldReturnNull()
    {
        var result = Library.ParseKeyValue("key=value", null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenEmptyValueProvided_ShouldReturnNull()
    {
        var result = Library.ParseKeyValue(string.Empty, "key");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenEmptyKeyProvided_ShouldReturnNull()
    {
        var result = Library.ParseKeyValue("key=value", string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenDefaultDelimiters_ShouldParse()
    {
        var result = Library.ParseKeyValue("name=John&age=30", "name");

        Assert.AreEqual("John", result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenCustomDelimiters_ShouldParse()
    {
        var result = Library.ParseKeyValue("name:John;age:30", "age", ";", ":");

        Assert.AreEqual("30", result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenKeyNotFound_ShouldReturnNull()
    {
        var result = Library.ParseKeyValue("name=John&age=30", "email");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseKeyValue_WhenValueHasDelimiter_ShouldReturnFullValue()
    {
        var result = Library.ParseKeyValue("url=http://example.com?foo=bar&other=1", "url");

        Assert.AreEqual("http://example.com?foo=bar", result);
    }

    #endregion

    #region FormatJson Tests

    [TestMethod]
    public void FormatJson_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.FormatJson(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatJson_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.FormatJson(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatJson_WhenValidJsonProvided_ShouldFormat()
    {
        var result = Library.FormatJson("{\"name\":\"John\",\"age\":30}");

        Assert.IsNotNull(result);
        Assert.Contains("\n", result);
        Assert.Contains("  ", result);
    }

    [TestMethod]
    public void FormatJson_WhenInvalidJsonProvided_ShouldReturnOriginal()
    {
        var original = "not valid json";
        var result = Library.FormatJson(original);

        Assert.AreEqual(original, result);
    }

    [TestMethod]
    public void FormatJson_WhenArrayProvided_ShouldFormat()
    {
        var result = Library.FormatJson("[1,2,3]");

        Assert.IsNotNull(result);
        Assert.Contains("\n", result);
    }

    #endregion

    #region MinifyJson Tests

    [TestMethod]
    public void MinifyJson_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.MinifyJson(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void MinifyJson_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.MinifyJson(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void MinifyJson_WhenFormattedJsonProvided_ShouldMinify()
    {
        var formatted = "{\n  \"name\": \"John\",\n  \"age\": 30\n}";
        var result = Library.MinifyJson(formatted);

        Assert.IsNotNull(result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("  ", result);
    }

    [TestMethod]
    public void MinifyJson_WhenInvalidJsonProvided_ShouldReturnOriginal()
    {
        var original = "not valid json";
        var result = Library.MinifyJson(original);

        Assert.AreEqual(original, result);
    }

    #endregion

    #region FormatXml Tests

    [TestMethod]
    public void FormatXml_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.FormatXml(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatXml_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.FormatXml(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void FormatXml_WhenValidXmlProvided_ShouldFormat()
    {
        var result = Library.FormatXml("<root><child>value</child></root>");

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\n") || result.Contains("  "));
    }

    [TestMethod]
    public void FormatXml_WhenInvalidXmlProvided_ShouldReturnOriginal()
    {
        var original = "not valid xml";
        var result = Library.FormatXml(original);

        Assert.AreEqual(original, result);
    }

    #endregion

    #region MinifyXml Tests

    [TestMethod]
    public void MinifyXml_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.MinifyXml(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void MinifyXml_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.MinifyXml(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void MinifyXml_WhenFormattedXmlProvided_ShouldMinify()
    {
        var formatted = "<root>\n  <child>value</child>\n</root>";
        var result = Library.MinifyXml(formatted);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MinifyXml_WhenInvalidXmlProvided_ShouldReturnOriginal()
    {
        var original = "not valid xml";
        var result = Library.MinifyXml(original);

        Assert.AreEqual(original, result);
    }

    #endregion

    #region ToHumanReadableSize Tests

    [TestMethod]
    public void ToHumanReadableSize_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToHumanReadableSize(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenZeroProvided_ShouldReturnZeroB()
    {
        var result = Library.ToHumanReadableSize(0);

        Assert.AreEqual("0 B", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenBytesProvided_ShouldReturnBytes()
    {
        var result = Library.ToHumanReadableSize(500);

        Assert.AreEqual("500 B", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenKilobytesProvided_ShouldReturnKB()
    {
        var result = Library.ToHumanReadableSize(1536);

        Assert.IsNotNull(result);
        Assert.Contains("KB", result);
        Assert.Contains("1", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenMegabytesProvided_ShouldReturnMB()
    {
        var result = Library.ToHumanReadableSize(1048576);

        Assert.AreEqual("1 MB", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenGigabytesProvided_ShouldReturnGB()
    {
        var result = Library.ToHumanReadableSize(1073741824L);

        Assert.AreEqual("1 GB", result);
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenTerabytesProvided_ShouldReturnTB()
    {
        var result = Library.ToHumanReadableSize(1099511627776L);

        Assert.AreEqual("1 TB", result);
    }

    #endregion

    #region ToHumanReadableDuration Tests

    [TestMethod]
    public void ToHumanReadableDuration_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToHumanReadableDuration(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenZeroProvided_ShouldReturn0s()
    {
        var result = Library.ToHumanReadableDuration(0);

        Assert.AreEqual("0s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenSecondsOnlyProvided_ShouldReturnSeconds()
    {
        var result = Library.ToHumanReadableDuration(45);

        Assert.AreEqual("45s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenMinutesProvided_ShouldReturnMinutesAndSeconds()
    {
        var result = Library.ToHumanReadableDuration(90);

        Assert.AreEqual("1m 30s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenHoursProvided_ShouldReturnHoursMinutesSeconds()
    {
        var result = Library.ToHumanReadableDuration(3661);

        Assert.AreEqual("1h 1m 1s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenDaysProvided_ShouldReturnDaysHoursMinutesSeconds()
    {
        var result = Library.ToHumanReadableDuration(90061);

        Assert.AreEqual("1d 1h 1m 1s", result);
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenExactHourProvided_ShouldNotShowZeroComponents()
    {
        var result = Library.ToHumanReadableDuration(3600);

        Assert.AreEqual("1h", result);
    }

    #endregion

    #region CalculateEntropy Tests

    [TestMethod]
    public void CalculateEntropy_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.CalculateEntropy(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateEntropy_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.CalculateEntropy(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void CalculateEntropy_WhenSingleCharRepeated_ShouldReturnZero()
    {
        var result = Library.CalculateEntropy("aaaa");

        Assert.IsNotNull(result);
        Assert.IsLessThan(0.001, Math.Abs(result.Value - 0.0));
    }

    [TestMethod]
    public void CalculateEntropy_WhenAllUniqueChars_ShouldReturnHighEntropy()
    {
        var result = Library.CalculateEntropy("abcd");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(1.5, result.Value);
    }

    [TestMethod]
    public void CalculateEntropy_WhenRandomStringProvided_ShouldReturnPositiveValue()
    {
        var result = Library.CalculateEntropy("Hello World!");

        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.Value);
    }

    #endregion

    #region IsBase64 Tests

    [TestMethod]
    public void IsBase64_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsBase64(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsBase64_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsBase64(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsBase64_WhenValidBase64Provided_ShouldReturnTrue()
    {
        var result = Library.IsBase64("SGVsbG8gV29ybGQh");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBase64_WhenValidBase64WithPaddingProvided_ShouldReturnTrue()
    {
        var result = Library.IsBase64("SGVsbG8=");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsBase64_WhenInvalidLengthProvided_ShouldReturnFalse()
    {
        var result = Library.IsBase64("abc");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsBase64_WhenInvalidCharsProvided_ShouldReturnFalse()
    {
        var result = Library.IsBase64("!!!!");

        Assert.IsFalse(result);
    }

    #endregion

    #region IsHex Tests

    [TestMethod]
    public void IsHex_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsHex(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsHex_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsHex(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsHex_WhenValidHexLowercaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("0123456789abcdef");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenValidHexUppercaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("0123456789ABCDEF");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenMixedCaseProvided_ShouldReturnTrue()
    {
        var result = Library.IsHex("AbCdEf");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsHex_WhenInvalidCharsProvided_ShouldReturnFalse()
    {
        var result = Library.IsHex("GHIJKL");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsHex_WhenMixedValidInvalidProvided_ShouldReturnFalse()
    {
        var result = Library.IsHex("ABC123XYZ");

        Assert.IsFalse(result);
    }

    #endregion

    #region IsJwt Tests

    [TestMethod]
    public void IsJwt_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.IsJwt(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsJwt_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.IsJwt(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void IsJwt_WhenValidJwtProvided_ShouldReturnTrue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var result = Library.IsJwt(jwt);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsJwt_WhenOnlyTwoPartsProvided_ShouldReturnFalse()
    {
        var result = Library.IsJwt("part1.part2");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenFourPartsProvided_ShouldReturnFalse()
    {
        var result = Library.IsJwt("part1.part2.part3.part4");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenInvalidBase64InParts_ShouldReturnFalse()
    {
        var result = Library.IsJwt("!!!.!!!.!!!");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsJwt_WhenNotAJwtFormat_ShouldReturnFalse()
    {
        var result = Library.IsJwt("this is not a jwt");

        Assert.IsFalse(result);
    }

    #endregion
}