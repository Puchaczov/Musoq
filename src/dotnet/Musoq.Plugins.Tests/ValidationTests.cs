using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class ValidationTests : PluginsTestBase
{
    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("test@example.com", true)]
    [DataRow("user@mail.example.com", true)]
    [DataRow("test+label@example.com", true)]
    [DataRow("testexample.com", false)]
    [DataRow("test@", false)]
    public void IsValidEmail_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidEmail(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("http://example.com", true)]
    [DataRow("https://example.com", true)]
    [DataRow("ftp://example.com", true)]
    [DataRow("ftp://files.example.com", true)]
    [DataRow("ftps://example.com", true)]
    [DataRow("https://example.com/path/to/resource", true)]
    [DataRow("https://example.com?query=value", true)]
    [DataRow("/path/to/resource", false)]
    [DataRow("mailto:test@example.com", false)]
    [DataRow("not a url", false)]
    public void IsValidUrl_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidUrl(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("http://example.com", true)]
    [DataRow("mailto:test@example.com", true)]
    [DataRow("file:///c:/path/to/file", true)]
    [DataRow("file:///c:/path", true)]
    [DataRow("not a uri", false)]
    public void IsValidUri_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidUri(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("{\"key\": \"value\"}", true)]
    [DataRow("[1, 2, 3]", true)]
    [DataRow("\"hello\"", true)]
    [DataRow("123.45", true)]
    [DataRow("true", true)]
    [DataRow("null", true)]
    [DataRow("{key: value}", false)]
    [DataRow("{\"key\": \"value\"", false)]
    [DataRow("{invalid json}", false)]
    public void IsValidJson_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidJson(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("<root>content</root>", true)]
    [DataRow("<root attr=\"value\">content</root>", true)]
    [DataRow("<root><child>content</child></root>", true)]
    [DataRow("<root><child>text</child></root>", true)]
    [DataRow("<?xml version=\"1.0\"?><root/>", true)]
    [DataRow("<root>", false)]
    [DataRow("<root></other>", false)]
    [DataRow("<root><unclosed>", false)]
    public void IsValidXml_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidXml(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("550e8400-e29b-41d4-a716-446655440000", true)]
    [DataRow("550e8400e29b41d4a716446655440000", true)]
    [DataRow("{550e8400-e29b-41d4-a716-446655440000}", true)]
    [DataRow("not-a-guid", false)]
    public void IsValidGuid_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidGuid(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("12345", true)]
    [DataRow("-12345", true)]
    [DataRow("0", true)]
    [DataRow("12.34", false)]
    [DataRow("abc", false)]
    public void IsValidInteger_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidInteger(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("12345", true)]
    [DataRow("123,456", true)]
    [DataRow("-123,456", true)]
    [DataRow("12,34", true)]
    [DataRow("-12,34", true)]
    [DataRow("abc", false)]
    public void IsValidDecimal_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidDecimal(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("2023-12-25T10:30:00", true)]
    [DataRow("2023-12-25", true)]
    [DataRow("2024-01-15T10:30:00Z", true)]
    [DataRow("2024-01-15", true)]
    [DataRow("not-a-date", false)]
    [DataRow("not a date", false)]
    public void IsValidDateTime_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidDateTime(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("192.168.1.1", true)]
    [DataRow("127.0.0.1", true)]
    [DataRow("0.0.0.0", true)]
    [DataRow("255.255.255.255", true)]
    [DataRow("192.168.1", false)]
    [DataRow("192.168.1.256", false)]
    [DataRow("192.168.-1.1", false)]
    [DataRow("192.168.1.1.1", false)]
    [DataRow("192.168.1.abc", false)]
    public void IsValidIPv4_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidIPv4(value));
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", false)]
    [DataRow("   ", false)]
    [DataRow("true", true)]
    [DataRow("TRUE", true)]
    [DataRow("True", true)]
    [DataRow("false", true)]
    [DataRow("FALSE", true)]
    [DataRow("yes", true)]
    [DataRow("no", true)]
    [DataRow("1", true)]
    [DataRow("0", true)]
    [DataRow("maybe", false)]
    [DataRow("2", false)]
    [DataRow("  true  ", true)]
    public void IsValidBoolean_WhenValueProvided_ReturnsExpected(string? value, bool? expected)
    {
        Assert.AreEqual(expected, Library.IsValidBoolean(value));
    }
}
