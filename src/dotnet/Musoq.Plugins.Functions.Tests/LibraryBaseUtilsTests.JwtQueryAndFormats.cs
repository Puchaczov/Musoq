using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseUtilsTests
{
    #region JWT Tests

    [TestMethod]
    public void JwtDecode_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.JwtDecode(null));
    }

    [TestMethod]
    public void JwtDecode_WhenValidJwt_ReturnsPayload()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var payload = _library.JwtDecode(jwt);
        Assert.IsNotNull(payload);
        Assert.Contains("John Doe", payload);
    }

    [TestMethod]
    public void JwtGetHeader_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.JwtGetHeader(null));
    }

    [TestMethod]
    public void JwtGetHeader_WhenValidJwt_ReturnsHeader()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var header = _library.JwtGetHeader(jwt);
        Assert.IsNotNull(header);
        Assert.Contains("HS256", header);
    }

    [TestMethod]
    public void JwtGetClaim_WhenValidJwt_ReturnsClaim()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var name = _library.JwtGetClaim(jwt, "name");
        Assert.AreEqual("John Doe", name);
    }

    [TestMethod]
    public void IsJwt_WhenValidJwt_ReturnsTrue()
    {
        var jwt =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        Assert.IsTrue(_library.IsJwt(jwt));
    }

    [TestMethod]
    public void IsJwt_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsJwt("not.a.jwt"));
        Assert.IsFalse(_library.IsJwt("just some text"));
    }

    #endregion

    #region Query String Tests

    [TestMethod]
    public void GetQueryParam_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.GetQueryParam(null, "key"));
    }

    [TestMethod]
    public void GetQueryParam_WhenValidQuery_ReturnsValue()
    {
        Assert.AreEqual("123", _library.GetQueryParam("?id=123&name=test", "id"));
        Assert.AreEqual("test", _library.GetQueryParam("id=123&name=test", "name"));
    }

    [TestMethod]
    public void ParseKeyValue_WhenValid_ReturnsValue()
    {
        Assert.AreEqual("bar", _library.ParseKeyValue("foo=bar&baz=qux", "foo"));
    }

    #endregion

    #region Format Tests

    [TestMethod]
    public void FormatJson_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatJson(null));
    }

    [TestMethod]
    public void FormatJson_WhenValid_FormatsWithIndentation()
    {
        var result = _library.FormatJson("{\"a\":1}");
        Assert.IsNotNull(result);
        Assert.Contains("\n", result);
    }

    [TestMethod]
    public void MinifyJson_WhenValid_RemovesWhitespace()
    {
        var result = _library.MinifyJson("{\n  \"a\": 1\n}");
        Assert.AreEqual("{\"a\":1}", result);
    }

    [TestMethod]
    public void FormatXml_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.FormatXml(null));
    }

    [TestMethod]
    public void MinifyXml_WhenValid_RemovesWhitespace()
    {
        var result = _library.MinifyXml("<root>\n  <child />\n</root>");
        Assert.IsNotNull(result);
        Assert.DoesNotContain("\n", result);
    }

    #endregion

    #region Human Readable Tests

    [TestMethod]
    public void ToHumanReadableSize_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToHumanReadableSize(null));
    }

    [TestMethod]
    public void ToHumanReadableSize_WhenBytes_ReturnsCorrect()
    {
        Assert.AreEqual("500 B", _library.ToHumanReadableSize(500));
        Assert.AreEqual("1 KB", _library.ToHumanReadableSize(1024));

        var result = _library.ToHumanReadableSize(1536);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith('1') && result.EndsWith("KB", System.StringComparison.Ordinal));
        Assert.AreEqual("1 MB", _library.ToHumanReadableSize(1024 * 1024));
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.ToHumanReadableDuration(null));
    }

    [TestMethod]
    public void ToHumanReadableDuration_WhenSeconds_ReturnsCorrect()
    {
        Assert.AreEqual("0s", _library.ToHumanReadableDuration(0));
        Assert.AreEqual("45s", _library.ToHumanReadableDuration(45));
        Assert.AreEqual("1m 30s", _library.ToHumanReadableDuration(90));
        Assert.AreEqual("1h 1m 1s", _library.ToHumanReadableDuration(3661));
        Assert.AreEqual("1d 1h", _library.ToHumanReadableDuration(90000));
    }

    #endregion

    #region Data Analysis Tests

    [TestMethod]
    public void CalculateEntropy_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.CalculateEntropy(null));
    }

    [TestMethod]
    public void CalculateEntropy_WhenAllSame_ReturnsZero()
    {
        Assert.AreEqual(0.0, _library.CalculateEntropy("aaaa"));
    }

    [TestMethod]
    public void CalculateEntropy_WhenRandom_ReturnsHigher()
    {
        var lowEntropy = _library.CalculateEntropy("aaaa");
        var highEntropy = _library.CalculateEntropy("abcd");
        Assert.IsNotNull(highEntropy);
        Assert.IsNotNull(lowEntropy);
        Assert.IsGreaterThan(lowEntropy.Value, highEntropy.Value);
    }

    [TestMethod]
    public void IsBase64_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsBase64(null));
    }

    [TestMethod]
    public void IsBase64_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsBase64("SGVsbG8gV29ybGQ="));
    }

    [TestMethod]
    public void IsBase64_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsBase64("not base64!"));
    }

    [TestMethod]
    public void IsHex_WhenNull_ReturnsNull()
    {
        Assert.IsNull(_library.IsHex(null));
    }

    [TestMethod]
    public void IsHex_WhenValid_ReturnsTrue()
    {
        Assert.IsTrue(_library.IsHex("48656c6c6f"));
        Assert.IsTrue(_library.IsHex("ABCDEF0123"));
    }

    [TestMethod]
    public void IsHex_WhenInvalid_ReturnsFalse()
    {
        Assert.IsFalse(_library.IsHex("xyz123"));
    }

    #endregion
}
