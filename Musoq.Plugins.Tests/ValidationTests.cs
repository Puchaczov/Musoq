using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ValidationTests
{
    private readonly LibraryBase _library = new();

    #region IsValidEmail Tests

    [TestMethod]
    public void IsValidEmail_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidEmail(null));
    }

    [TestMethod]
    public void IsValidEmail_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidEmail(string.Empty));
    }

    [TestMethod]
    public void IsValidEmail_WhenWhitespace_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidEmail("   "));
    }

    [TestMethod]
    public void IsValidEmail_WhenValidSimple_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidEmail("test@example.com"));
    }

    [TestMethod]
    public void IsValidEmail_WhenValidWithSubdomain_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidEmail("user@mail.example.com"));
    }

    [TestMethod]
    public void IsValidEmail_WhenValidWithPlus_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidEmail("test+label@example.com"));
    }

    [TestMethod]
    public void IsValidEmail_WhenMissingAt_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidEmail("testexample.com"));
    }

    [TestMethod]
    public void IsValidEmail_WhenMissingDomain_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidEmail("test@"));
    }

    #endregion

    #region IsValidUrl Tests

    [TestMethod]
    public void IsValidUrl_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidUrl(null));
    }

    [TestMethod]
    public void IsValidUrl_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidUrl(string.Empty));
    }

    [TestMethod]
    public void IsValidUrl_WhenHttp_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUrl("http://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_WhenHttps_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUrl("https://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_WhenFtp_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUrl("ftp://files.example.com"));
    }

    [TestMethod]
    public void IsValidUrl_WhenWithPath_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUrl("https://example.com/path/to/resource"));
    }

    [TestMethod]
    public void IsValidUrl_WhenWithQueryString_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUrl("https://example.com?query=value"));
    }

    [TestMethod]
    public void IsValidUrl_WhenRelative_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidUrl("/path/to/resource"));
    }

    [TestMethod]
    public void IsValidUrl_WhenInvalidScheme_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidUrl("mailto:test@example.com"));
    }

    #endregion

    #region IsValidUri Tests

    [TestMethod]
    public void IsValidUri_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidUri(null));
    }

    [TestMethod]
    public void IsValidUri_WhenMailto_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUri("mailto:test@example.com"));
    }

    [TestMethod]
    public void IsValidUri_WhenFile_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidUri("file:///c:/path/to/file"));
    }

    #endregion

    #region IsValidJson Tests

    [TestMethod]
    public void IsValidJson_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidJson(null));
    }

    [TestMethod]
    public void IsValidJson_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidJson(string.Empty));
    }

    [TestMethod]
    public void IsValidJson_WhenValidObject_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("{\"key\": \"value\"}"));
    }

    [TestMethod]
    public void IsValidJson_WhenValidArray_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("[1, 2, 3]"));
    }

    [TestMethod]
    public void IsValidJson_WhenValidString_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("\"hello\""));
    }

    [TestMethod]
    public void IsValidJson_WhenValidNumber_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("123.45"));
    }

    [TestMethod]
    public void IsValidJson_WhenValidBoolean_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("true"));
    }

    [TestMethod]
    public void IsValidJson_WhenValidNull_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidJson("null"));
    }

    [TestMethod]
    public void IsValidJson_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidJson("{key: value}"));
    }

    [TestMethod]
    public void IsValidJson_WhenUnclosed_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidJson("{\"key\": \"value\""));
    }

    #endregion

    #region IsValidXml Tests

    [TestMethod]
    public void IsValidXml_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidXml(null));
    }

    [TestMethod]
    public void IsValidXml_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidXml(string.Empty));
    }

    [TestMethod]
    public void IsValidXml_WhenValidSimple_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidXml("<root>content</root>"));
    }

    [TestMethod]
    public void IsValidXml_WhenValidWithAttributes_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidXml("<root attr=\"value\">content</root>"));
    }

    [TestMethod]
    public void IsValidXml_WhenValidNested_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidXml("<root><child>content</child></root>"));
    }

    [TestMethod]
    public void IsValidXml_WhenValidWithDeclaration_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidXml("<?xml version=\"1.0\"?><root/>"));
    }

    [TestMethod]
    public void IsValidXml_WhenUnclosedTag_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidXml("<root>"));
    }

    [TestMethod]
    public void IsValidXml_WhenMismatchedTags_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidXml("<root></other>"));
    }

    #endregion

    #region IsValidGuid Tests

    [TestMethod]
    public void IsValidGuid_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidGuid(null));
    }

    [TestMethod]
    public void IsValidGuid_WhenEmpty_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidGuid(string.Empty));
    }

    [TestMethod]
    public void IsValidGuid_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidGuid("550e8400-e29b-41d4-a716-446655440000"));
    }

    [TestMethod]
    public void IsValidGuid_WhenValidNoBraces_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidGuid("550e8400e29b41d4a716446655440000"));
    }

    [TestMethod]
    public void IsValidGuid_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidGuid("not-a-guid"));
    }

    #endregion

    #region IsValidInteger Tests

    [TestMethod]
    public void IsValidInteger_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidInteger(null));
    }

    [TestMethod]
    public void IsValidInteger_WhenPositive_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidInteger("12345"));
    }

    [TestMethod]
    public void IsValidInteger_WhenNegative_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidInteger("-12345"));
    }

    [TestMethod]
    public void IsValidInteger_WhenZero_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidInteger("0"));
    }

    [TestMethod]
    public void IsValidInteger_WhenDecimal_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidInteger("12.34"));
    }

    [TestMethod]
    public void IsValidInteger_WhenText_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidInteger("abc"));
    }

    #endregion

    #region IsValidDecimal Tests

    [TestMethod]
    public void IsValidDecimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidDecimal(null));
    }

    [TestMethod]
    public void IsValidDecimal_WhenInteger_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidDecimal("12345"));
    }

    [TestMethod]
    public void IsValidDecimal_WhenDecimal_ReturnsTrue()
    {
        // Use comma as decimal separator for culture-independent test
        Assert.IsTrue(_library.IsValidDecimal("123,456"));
    }

    [TestMethod]
    public void IsValidDecimal_WhenNegative_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidDecimal("-123,456"));
    }

    [TestMethod]
    public void IsValidDecimal_WhenText_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidDecimal("abc"));
    }

    #endregion

    #region IsValidDateTime Tests

    [TestMethod]
    public void IsValidDateTime_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidDateTime(null));
    }

    [TestMethod]
    public void IsValidDateTime_WhenValidIso_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidDateTime("2023-12-25T10:30:00"));
    }

    [TestMethod]
    public void IsValidDateTime_WhenValidDate_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidDateTime("2023-12-25"));
    }

    [TestMethod]
    public void IsValidDateTime_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidDateTime("not-a-date"));
    }

    #endregion

    #region IsValidIPv4 Tests

    [TestMethod]
    public void IsValidIPv4_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidIPv4(null));
    }

    [TestMethod]
    public void IsValidIPv4_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidIPv4("192.168.1.1"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenLocalhost_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidIPv4("127.0.0.1"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenZeros_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidIPv4("0.0.0.0"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenMax_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidIPv4("255.255.255.255"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenTooFewParts_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidIPv4("192.168.1"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenValueTooHigh_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidIPv4("192.168.1.256"));
    }

    [TestMethod]
    public void IsValidIPv4_WhenNegative_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidIPv4("192.168.-1.1"));
    }

    #endregion

    #region IsValidBoolean Tests

    [TestMethod]
    public void IsValidBoolean_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsValidBoolean(null));
    }

    [TestMethod]
    public void IsValidBoolean_WhenTrue_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidBoolean("true"));
        Assert.IsTrue(_library.IsValidBoolean("TRUE"));
        Assert.IsTrue(_library.IsValidBoolean("True"));
    }

    [TestMethod]
    public void IsValidBoolean_WhenFalse_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidBoolean("false"));
        Assert.IsTrue(_library.IsValidBoolean("FALSE"));
    }

    [TestMethod]
    public void IsValidBoolean_WhenYesNo_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidBoolean("yes"));
        Assert.IsTrue(_library.IsValidBoolean("no"));
    }

    [TestMethod]
    public void IsValidBoolean_WhenOneZero_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsValidBoolean("1"));
        Assert.IsTrue(_library.IsValidBoolean("0"));
    }

    [TestMethod]
    public void IsValidBoolean_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsValidBoolean("maybe"));
        Assert.IsFalse(_library.IsValidBoolean("2"));
    }

    #endregion
}
