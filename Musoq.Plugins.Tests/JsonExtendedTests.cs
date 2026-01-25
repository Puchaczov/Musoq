using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for JSON methods to improve branch coverage.
/// </summary>
[TestClass]
public class JsonExtendedTests : LibraryBaseBaseTests
{
    #region ToJson Tests

    [TestMethod]
    public void ToJson_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToJson<string>(null));
    }

    [TestMethod]
    public void ToJson_String_ReturnsJsonString()
    {
        var result = Library.ToJson("hello");
        Assert.AreEqual("\"hello\"", result);
    }

    [TestMethod]
    public void ToJson_Int_ReturnsJsonNumber()
    {
        var result = Library.ToJson(42);
        Assert.AreEqual("42", result);
    }

    [TestMethod]
    public void ToJson_Bool_ReturnsJsonBool()
    {
        Assert.AreEqual("true", Library.ToJson(true));
        Assert.AreEqual("false", Library.ToJson(false));
    }

    [TestMethod]
    public void ToJson_Array_ReturnsJsonArray()
    {
        var result = Library.ToJson(new[] { 1, 2, 3 });
        Assert.AreEqual("[1,2,3]", result);
    }

    [TestMethod]
    public void ToJson_Object_ReturnsJsonObject()
    {
        var result = Library.ToJson(new { Name = "Test", Value = 42 });
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"Name\":\"Test\""));
        Assert.IsTrue(result.Contains("\"Value\":42"));
    }

    #endregion

    #region ExtractFromJson Tests

    [TestMethod]
    public void ExtractFromJson_NullJson_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractFromJson(null, "$.name"));
    }

    [TestMethod]
    public void ExtractFromJson_EmptyJson_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractFromJson(string.Empty, "$.name"));
    }

    [TestMethod]
    public void ExtractFromJson_NullPath_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractFromJson("{\"name\":\"test\"}", null));
    }

    [TestMethod]
    public void ExtractFromJson_EmptyPath_ReturnsNull()
    {
        Assert.IsNull(Library.ExtractFromJson("{\"name\":\"test\"}", string.Empty));
    }

    [TestMethod]
    public void ExtractFromJson_ValidPathSimple_ReturnsValue()
    {
        var result = Library.ExtractFromJson("{\"name\":\"test\"}", "$.name");
        Assert.AreEqual("test", result);
    }

    #endregion

    #region ExtractFromJsonToArray Tests

    [TestMethod]
    public void ExtractFromJsonToArray_NullJson_ReturnsEmptyArray()
    {
        var result = Library.ExtractFromJsonToArray(null, "$.name");
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ExtractFromJsonToArray_EmptyJson_ReturnsEmptyArray()
    {
        var result = Library.ExtractFromJsonToArray(string.Empty, "$.name");
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ExtractFromJsonToArray_NullPath_ReturnsEmptyArray()
    {
        var result = Library.ExtractFromJsonToArray("{\"name\":\"test\"}", null);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ExtractFromJsonToArray_EmptyPath_ReturnsEmptyArray()
    {
        var result = Library.ExtractFromJsonToArray("{\"name\":\"test\"}", string.Empty);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ExtractFromJsonToArray_ValidPath_ReturnsArray()
    {
        var result = Library.ExtractFromJsonToArray("{\"name\":\"test\"}", "$.name");
        Assert.IsTrue(result.Length > 0);
        Assert.AreEqual("test", result[0]);
    }

    [TestMethod]
    public void ExtractFromJsonToArray_ArrayPath_ReturnsMultipleValues()
    {
        var result = Library.ExtractFromJsonToArray("{\"names\":[\"a\",\"b\",\"c\"]}", "$.names[*]");
        Assert.AreEqual(3, result.Length);
    }

    #endregion
}
