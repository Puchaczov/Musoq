using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class DataUtilsTests
{
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
        Assert.IsTrue(result.Contains('\n') || result.Contains("  "));
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
}
