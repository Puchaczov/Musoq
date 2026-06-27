using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class NetworkUtilsTests
{
    #region ToSlug Tests

    [TestMethod]
    public void ToSlug_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.ToSlug(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToSlug_WhenEmptyProvided_ShouldReturnNull()
    {
        var result = Library.ToSlug(string.Empty);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ToSlug_WhenSimpleStringProvided_ShouldConvert()
    {
        var result = Library.ToSlug("Hello World");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenSpecialCharsProvided_ShouldRemove()
    {
        var result = Library.ToSlug("Hello! World?");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenAccentsProvided_ShouldNormalize()
    {
        var result = Library.ToSlug("Café résumé");

        Assert.AreEqual("cafe-resume", result);
    }

    [TestMethod]
    public void ToSlug_WhenMultipleSpacesProvided_ShouldCollapse()
    {
        var result = Library.ToSlug("Hello   World");

        Assert.AreEqual("hello-world", result);
    }

    [TestMethod]
    public void ToSlug_WhenUnderscoresProvided_ShouldConvertToDashes()
    {
        var result = Library.ToSlug("hello_world");

        Assert.AreEqual("hello-world", result);
    }

    #endregion

    #region EscapeRegex Tests

    [TestMethod]
    public void EscapeRegex_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.EscapeRegex(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeRegex_WhenSpecialCharsProvided_ShouldEscape()
    {
        var result = Library.EscapeRegex("test.+*?");

        Assert.IsNotNull(result);
        Assert.Contains(@"\.", result);
        Assert.Contains(@"\+", result);
        Assert.Contains(@"\*", result);
        Assert.Contains(@"\?", result);
    }

    [TestMethod]
    public void EscapeRegex_WhenNoSpecialChars_ShouldReturnSame()
    {
        var result = Library.EscapeRegex("hello");

        Assert.AreEqual("hello", result);
    }

    #endregion

    #region EscapeSql Tests

    [TestMethod]
    public void EscapeSql_WhenNullProvided_ShouldReturnNull()
    {
        var result = Library.EscapeSql(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void EscapeSql_WhenSingleQuotesProvided_ShouldDouble()
    {
        var result = Library.EscapeSql("It's a test");

        Assert.AreEqual("It''s a test", result);
    }

    [TestMethod]
    public void EscapeSql_WhenNoQuotes_ShouldReturnSame()
    {
        var result = Library.EscapeSql("hello world");

        Assert.AreEqual("hello world", result);
    }

    [TestMethod]
    public void EscapeSql_WhenMultipleQuotes_ShouldDoubleAll()
    {
        var result = Library.EscapeSql("'test'");

        Assert.AreEqual("''test''", result);
    }

    #endregion
}
