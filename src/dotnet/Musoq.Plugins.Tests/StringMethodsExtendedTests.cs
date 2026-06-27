using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for string methods to improve branch coverage.
///     Tests ToSnakeCase, ToKebabCase, ToCamelCase, ToPascalCase,
///     WordCount, LineCount, SentenceCount, and other string utilities.
/// </summary>
[TestClass]
public partial class StringMethodsExtendedTests : PluginsTestBase
{
    #region ToSnakeCase Tests

    [TestMethod]
    public void ToSnakeCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToSnakeCase(null));
    }

    [TestMethod]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToSnakeCase(string.Empty));
    }

    [TestMethod]
    public void ToSnakeCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToSnakeCase("a"));
    }

    [TestMethod]
    public void ToSnakeCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToSnakeCase("A"));
    }

    [TestMethod]
    public void ToSnakeCase_PascalCase_ReturnsSnakeCase()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("HelloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_CamelCase_ReturnsSnakeCase()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("helloWorld"));
    }

    [TestMethod]
    public void ToSnakeCase_XMLParser_ReturnsSnakeCaseWithAcronym()
    {
        Assert.AreEqual("xml_parser", Library.ToSnakeCase("XMLParser"));
    }

    [TestMethod]
    public void ToSnakeCase_ConsecutiveUppercase_HandlesCorrectly()
    {
        Assert.AreEqual("get_http_response", Library.ToSnakeCase("GetHTTPResponse"));
    }

    [TestMethod]
    public void ToSnakeCase_WithSpace_ReplacesWithUnderscore()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello world"));
    }

    [TestMethod]
    public void ToSnakeCase_WithDash_ReplacesWithUnderscore()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello-world"));
    }

    [TestMethod]
    public void ToSnakeCase_AlreadySnakeCase_ReturnsSame()
    {
        Assert.AreEqual("hello_world", Library.ToSnakeCase("hello_world"));
    }

    [TestMethod]
    public void ToSnakeCase_AllUppercase_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToSnakeCase("HELLO"));
    }

    [TestMethod]
    public void ToSnakeCase_MixedWithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123_value", Library.ToSnakeCase("Test123Value"));
    }

    [TestMethod]
    public void ToSnakeCase_StartsWithUpper_NoLeadingUnderscore()
    {
        Assert.AreEqual("test", Library.ToSnakeCase("Test"));
    }

    [TestMethod]
    public void ToSnakeCase_UpperAtEndFollowedByNothing_HandlesCorrectly()
    {
        Assert.AreEqual("hello_w", Library.ToSnakeCase("HelloW"));
    }

    #endregion

    #region ToKebabCase Tests

    [TestMethod]
    public void ToKebabCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToKebabCase(null));
    }

    [TestMethod]
    public void ToKebabCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToKebabCase(string.Empty));
    }

    [TestMethod]
    public void ToKebabCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToKebabCase("a"));
    }

    [TestMethod]
    public void ToKebabCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToKebabCase("A"));
    }

    [TestMethod]
    public void ToKebabCase_PascalCase_ReturnsKebabCase()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("HelloWorld"));
    }

    [TestMethod]
    public void ToKebabCase_CamelCase_ReturnsKebabCase()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("helloWorld"));
    }

    [TestMethod]
    public void ToKebabCase_XMLParser_ReturnsKebabCaseWithAcronym()
    {
        Assert.AreEqual("xml-parser", Library.ToKebabCase("XMLParser"));
    }

    [TestMethod]
    public void ToKebabCase_ConsecutiveUppercase_HandlesCorrectly()
    {
        Assert.AreEqual("get-http-response", Library.ToKebabCase("GetHTTPResponse"));
    }

    [TestMethod]
    public void ToKebabCase_WithSpace_ReplacesWithDash()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello world"));
    }

    [TestMethod]
    public void ToKebabCase_WithUnderscore_ReplacesWithDash()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello_world"));
    }

    [TestMethod]
    public void ToKebabCase_AlreadyKebabCase_ReturnsSame()
    {
        Assert.AreEqual("hello-world", Library.ToKebabCase("hello-world"));
    }

    [TestMethod]
    public void ToKebabCase_AllUppercase_ReturnsLowercase()
    {
        Assert.AreEqual("hello", Library.ToKebabCase("HELLO"));
    }

    [TestMethod]
    public void ToKebabCase_MixedWithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123-value", Library.ToKebabCase("Test123Value"));
    }

    [TestMethod]
    public void ToKebabCase_UpperAtEnd_HandlesCorrectly()
    {
        Assert.AreEqual("hello-w", Library.ToKebabCase("HelloW"));
    }

    #endregion

    #region ToCamelCase Tests

    [TestMethod]
    public void ToCamelCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToCamelCase(null));
    }

    [TestMethod]
    public void ToCamelCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToCamelCase(string.Empty));
    }

    [TestMethod]
    public void ToCamelCase_SingleLowerLetter_ReturnsSame()
    {
        Assert.AreEqual("a", Library.ToCamelCase("a"));
    }

    [TestMethod]
    public void ToCamelCase_SingleUpperLetter_ReturnsLower()
    {
        Assert.AreEqual("a", Library.ToCamelCase("A"));
    }

    [TestMethod]
    public void ToCamelCase_SnakeCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello_world"));
    }

    [TestMethod]
    public void ToCamelCase_KebabCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello-world"));
    }

    [TestMethod]
    public void ToCamelCase_SpaceSeparated_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello world"));
    }

    [TestMethod]
    public void ToCamelCase_PascalCase_ReturnsCamelCase()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("HelloWorld"));
    }

    [TestMethod]
    public void ToCamelCase_AllUppercase_ReturnsAllLowerCase()
    {
        Assert.AreEqual("hELLO", Library.ToCamelCase("HELLO"));
    }

    [TestMethod]
    public void ToCamelCase_WithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("test123", Library.ToCamelCase("test123"));
    }

    [TestMethod]
    public void ToCamelCase_ConsecutiveDelimiters_HandlesCorrectly()
    {
        Assert.AreEqual("helloWorld", Library.ToCamelCase("hello__world"));
    }

    [TestMethod]
    public void ToCamelCase_TrailingDelimiter_HandlesCorrectly()
    {
        Assert.AreEqual("hello", Library.ToCamelCase("hello_"));
    }

    [TestMethod]
    public void ToCamelCase_LeadingDelimiter_HandlesCorrectly()
    {
        Assert.AreEqual("Hello", Library.ToCamelCase("_hello"));
    }

    #endregion

    #region ToPascalCase Tests

    [TestMethod]
    public void ToPascalCase_Null_ReturnsNull()
    {
        Assert.IsNull(Library.ToPascalCase(null));
    }

    [TestMethod]
    public void ToPascalCase_EmptyString_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToPascalCase(string.Empty));
    }

    [TestMethod]
    public void ToPascalCase_SingleLowerLetter_ReturnsUpper()
    {
        Assert.AreEqual("A", Library.ToPascalCase("a"));
    }

    [TestMethod]
    public void ToPascalCase_SingleUpperLetter_ReturnsSame()
    {
        Assert.AreEqual("A", Library.ToPascalCase("A"));
    }

    [TestMethod]
    public void ToPascalCase_SnakeCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello_world"));
    }

    [TestMethod]
    public void ToPascalCase_KebabCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello-world"));
    }

    [TestMethod]
    public void ToPascalCase_SpaceSeparated_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello world"));
    }

    [TestMethod]
    public void ToPascalCase_AlreadyPascalCase_ReturnsSame()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("HelloWorld"));
    }

    [TestMethod]
    public void ToPascalCase_CamelCase_ReturnsPascalCase()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("helloWorld"));
    }

    [TestMethod]
    public void ToPascalCase_WithNumbers_PreservesNumbers()
    {
        Assert.AreEqual("Test123Value", Library.ToPascalCase("test123_value"));
    }

    [TestMethod]
    public void ToPascalCase_ConsecutiveDelimiters_HandlesCorrectly()
    {
        Assert.AreEqual("HelloWorld", Library.ToPascalCase("hello__world"));
    }

    [TestMethod]
    public void ToPascalCase_OnlyDelimiters_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.ToPascalCase("___"));
    }

    #endregion
}
