using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class StringsTests
{
    #region Html Encoding Tests

    [TestMethod]
    public void HtmlEncode_WhenSpecialCharacters_ShouldEncodeCorrectly()
    {
        var result = Library.HtmlEncode("<script>alert('test')</script>");

        Assert.AreEqual("&lt;script&gt;alert(&#39;test&#39;)&lt;/script&gt;", result);
    }

    [TestMethod]
    public void HtmlEncode_WhenNull_ShouldReturnNull()
    {
        var result = Library.HtmlEncode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HtmlEncode_WhenAmpersand_ShouldEncode()
    {
        var result = Library.HtmlEncode("Tom & Jerry");

        Assert.AreEqual("Tom &amp; Jerry", result);
    }

    [TestMethod]
    public void HtmlDecode_WhenEncodedCharacters_ShouldDecodeCorrectly()
    {
        var result = Library.HtmlDecode("&lt;script&gt;alert(&#39;test&#39;)&lt;/script&gt;");

        Assert.AreEqual("<script>alert('test')</script>", result);
    }

    [TestMethod]
    public void HtmlDecode_WhenNull_ShouldReturnNull()
    {
        var result = Library.HtmlDecode(null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void HtmlDecode_WhenAmpersand_ShouldDecode()
    {
        var result = Library.HtmlDecode("Tom &amp; Jerry");

        Assert.AreEqual("Tom & Jerry", result);
    }

    [TestMethod]
    public void HtmlRoundTrip_ShouldPreserveContent()
    {
        const string original = "<div class=\"test\">Hello & Goodbye</div>";

        var encoded = Library.HtmlEncode(original);
        var decoded = Library.HtmlDecode(encoded);

        Assert.AreEqual(original, decoded);
    }

    #endregion
}
