using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for validation methods to improve branch coverage.
///     Tests IsValidEmail, IsValidUrl, IsValidUri, IsValidJson, IsValidXml,
///     IsValidGuid, IsValidInteger, IsValidDecimal, IsValidDateTime, IsValidIPv4, IsValidBoolean.
/// </summary>
[TestClass]
public class ValidationMethodsExtendedTests : LibraryBaseBaseTests
{
    #region IsValidEmail Tests

    [TestMethod]
    public void IsValidEmail_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidEmail(null));
    }

    [TestMethod]
    public void IsValidEmail_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidEmail(string.Empty));
    }

    [TestMethod]
    public void IsValidEmail_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidEmail("   "));
    }

    [TestMethod]
    public void IsValidEmail_Valid_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidEmail("test@example.com"));
    }

    [TestMethod]
    public void IsValidEmail_ValidWithPlus_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidEmail("test+label@example.com"));
    }

    [TestMethod]
    public void IsValidEmail_InvalidNoAt_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidEmail("testexample.com"));
    }

    [TestMethod]
    public void IsValidEmail_InvalidNoDomain_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidEmail("test@"));
    }

    #endregion

    #region IsValidUrl Tests

    [TestMethod]
    public void IsValidUrl_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidUrl(null));
    }

    [TestMethod]
    public void IsValidUrl_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUrl(string.Empty));
    }

    [TestMethod]
    public void IsValidUrl_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUrl("   "));
    }

    [TestMethod]
    public void IsValidUrl_Http_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUrl("http://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_Https_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUrl("https://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_Ftp_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUrl("ftp://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_Ftps_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUrl("ftps://example.com"));
    }

    [TestMethod]
    public void IsValidUrl_InvalidScheme_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUrl("mailto:test@example.com"));
    }

    [TestMethod]
    public void IsValidUrl_NotUrl_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUrl("not a url"));
    }

    #endregion

    #region IsValidUri Tests

    [TestMethod]
    public void IsValidUri_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidUri(null));
    }

    [TestMethod]
    public void IsValidUri_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUri(string.Empty));
    }

    [TestMethod]
    public void IsValidUri_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUri("   "));
    }

    [TestMethod]
    public void IsValidUri_Http_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUri("http://example.com"));
    }

    [TestMethod]
    public void IsValidUri_Mailto_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUri("mailto:test@example.com"));
    }

    [TestMethod]
    public void IsValidUri_File_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidUri("file:///c:/path"));
    }

    [TestMethod]
    public void IsValidUri_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidUri("not a uri"));
    }

    #endregion

    #region IsValidJson Tests

    [TestMethod]
    public void IsValidJson_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidJson(null));
    }

    [TestMethod]
    public void IsValidJson_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidJson(string.Empty));
    }

    [TestMethod]
    public void IsValidJson_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidJson("   "));
    }

    [TestMethod]
    public void IsValidJson_ValidObject_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidJson("{\"key\": \"value\"}"));
    }

    [TestMethod]
    public void IsValidJson_ValidArray_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidJson("[1, 2, 3]"));
    }

    [TestMethod]
    public void IsValidJson_ValidPrimitive_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidJson("\"hello\""));
    }

    [TestMethod]
    public void IsValidJson_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidJson("{invalid json}"));
    }

    #endregion

    #region IsValidXml Tests

    [TestMethod]
    public void IsValidXml_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidXml(null));
    }

    [TestMethod]
    public void IsValidXml_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidXml(string.Empty));
    }

    [TestMethod]
    public void IsValidXml_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidXml("   "));
    }

    [TestMethod]
    public void IsValidXml_Valid_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidXml("<root><child>text</child></root>"));
    }

    [TestMethod]
    public void IsValidXml_ValidWithDeclaration_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidXml("<?xml version=\"1.0\"?><root/>"));
    }

    [TestMethod]
    public void IsValidXml_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidXml("<root><unclosed>"));
    }

    #endregion

    #region IsValidGuid Tests

    [TestMethod]
    public void IsValidGuid_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidGuid(null));
    }

    [TestMethod]
    public void IsValidGuid_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidGuid(string.Empty));
    }

    [TestMethod]
    public void IsValidGuid_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidGuid("   "));
    }

    [TestMethod]
    public void IsValidGuid_ValidWithHyphens_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidGuid("550e8400-e29b-41d4-a716-446655440000"));
    }

    [TestMethod]
    public void IsValidGuid_ValidNoBraces_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidGuid("550e8400e29b41d4a716446655440000"));
    }

    [TestMethod]
    public void IsValidGuid_ValidWithBraces_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidGuid("{550e8400-e29b-41d4-a716-446655440000}"));
    }

    [TestMethod]
    public void IsValidGuid_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidGuid("not-a-guid"));
    }

    #endregion

    #region IsValidInteger Tests

    [TestMethod]
    public void IsValidInteger_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidInteger(null));
    }

    [TestMethod]
    public void IsValidInteger_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidInteger(string.Empty));
    }

    [TestMethod]
    public void IsValidInteger_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidInteger("   "));
    }

    [TestMethod]
    public void IsValidInteger_ValidPositive_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidInteger("12345"));
    }

    [TestMethod]
    public void IsValidInteger_ValidNegative_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidInteger("-12345"));
    }

    [TestMethod]
    public void IsValidInteger_ValidZero_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidInteger("0"));
    }

    [TestMethod]
    public void IsValidInteger_Decimal_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidInteger("12.34"));
    }

    [TestMethod]
    public void IsValidInteger_Letters_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidInteger("abc"));
    }

    #endregion

    #region IsValidDecimal Tests

    [TestMethod]
    public void IsValidDecimal_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidDecimal(null));
    }

    [TestMethod]
    public void IsValidDecimal_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDecimal(string.Empty));
    }

    [TestMethod]
    public void IsValidDecimal_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDecimal("   "));
    }

    [TestMethod]
    public void IsValidDecimal_ValidInteger_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidDecimal("12345"));
    }

    [TestMethod]
    public void IsValidDecimal_ValidWithDecimal_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidDecimal("12,34"));
    }

    [TestMethod]
    public void IsValidDecimal_ValidNegative_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidDecimal("-12,34"));
    }

    [TestMethod]
    public void IsValidDecimal_Letters_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDecimal("abc"));
    }

    #endregion

    #region IsValidDateTime Tests

    [TestMethod]
    public void IsValidDateTime_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidDateTime(null));
    }

    [TestMethod]
    public void IsValidDateTime_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDateTime(string.Empty));
    }

    [TestMethod]
    public void IsValidDateTime_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDateTime("   "));
    }

    [TestMethod]
    public void IsValidDateTime_ValidIso_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidDateTime("2024-01-15T10:30:00Z"));
    }

    [TestMethod]
    public void IsValidDateTime_ValidDateOnly_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidDateTime("2024-01-15"));
    }

    [TestMethod]
    public void IsValidDateTime_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidDateTime("not a date"));
    }

    #endregion

    #region IsValidIPv4 Tests

    [TestMethod]
    public void IsValidIPv4_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidIPv4(null));
    }

    [TestMethod]
    public void IsValidIPv4_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4(string.Empty));
    }

    [TestMethod]
    public void IsValidIPv4_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4("   "));
    }

    [TestMethod]
    public void IsValidIPv4_Valid_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidIPv4("192.168.1.1"));
    }

    [TestMethod]
    public void IsValidIPv4_Localhost_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidIPv4("127.0.0.1"));
    }

    [TestMethod]
    public void IsValidIPv4_AllZeros_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidIPv4("0.0.0.0"));
    }

    [TestMethod]
    public void IsValidIPv4_MaxValues_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidIPv4("255.255.255.255"));
    }

    [TestMethod]
    public void IsValidIPv4_TooFewParts_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4("192.168.1"));
    }

    [TestMethod]
    public void IsValidIPv4_TooManyParts_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4("192.168.1.1.1"));
    }

    [TestMethod]
    public void IsValidIPv4_InvalidOctet_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4("192.168.1.256"));
    }

    [TestMethod]
    public void IsValidIPv4_NonNumericPart_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidIPv4("192.168.1.abc"));
    }

    #endregion

    #region IsValidBoolean Tests

    [TestMethod]
    public void IsValidBoolean_Null_ReturnsNull()
    {
        Assert.IsNull(Library.IsValidBoolean(null));
    }

    [TestMethod]
    public void IsValidBoolean_EmptyString_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidBoolean(string.Empty));
    }

    [TestMethod]
    public void IsValidBoolean_Whitespace_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidBoolean("   "));
    }

    [TestMethod]
    public void IsValidBoolean_True_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("true"));
    }

    [TestMethod]
    public void IsValidBoolean_False_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("false"));
    }

    [TestMethod]
    public void IsValidBoolean_TrueUppercase_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("TRUE"));
    }

    [TestMethod]
    public void IsValidBoolean_Yes_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("yes"));
    }

    [TestMethod]
    public void IsValidBoolean_No_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("no"));
    }

    [TestMethod]
    public void IsValidBoolean_One_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("1"));
    }

    [TestMethod]
    public void IsValidBoolean_Zero_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("0"));
    }

    [TestMethod]
    public void IsValidBoolean_Invalid_ReturnsFalse()
    {
        Assert.AreEqual(false, Library.IsValidBoolean("maybe"));
    }

    [TestMethod]
    public void IsValidBoolean_WithLeadingWhitespace_ReturnsTrue()
    {
        Assert.AreEqual(true, Library.IsValidBoolean("  true  "));
    }

    #endregion
}
